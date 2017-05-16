using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Principal;
using System.Diagnostics;
using System.IO.Pipes;
using Vaser.ConnectionSettings;

namespace Vaser
{
    internal class Connection
    {
        private object _DisposeLock = new object();
        public volatile bool IsServer = false;

        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        private Socket _SocketTCPClient;
        //private NamedPipeServerStream _NamedPipeServerStream;
        //private NamedPipeClientStream _NamedPipeClientStream;

        public bool StreamIsConnected
        {
            get; set;
        }


        public bool Disposed;
        public volatile bool BootupDone = false;

        internal delegate void QueueSendHidden();
        internal QueueSendHidden QueueSend = null;

        internal PortalCollection _PCollection = null;

        private int bytesRead;

        private byte[] _buff = new byte[65012];

        public static volatile bool IsInOnEmptyBufferQueue;
        private static object _CallOnEmptyBuffer_Lock = new object();
        private static Queue<LinkEventArgs> _CallOnEmptyBufferQueue = new Queue<LinkEventArgs>();

        private MemoryStream _rms2 = null;
        private BinaryReader _rbr2 = null;

        private VaserOptions _Mode = null;

        private VaserSSLServer _vSSLS = null;
        private VaserKerberosServer _vKerberosS = null;
        private VaserSSLClient _vSSLC = null;
        private VaserKerberosClient _vKerberosC = null;

        private bool IsInSendQueue = false;

        private AsyncCallback mySendNotEncryptedCallback = null;
        private AsyncCallback myReceiveNotEncryptedCallback = null;
        private AsyncCallback mySendKerberosCallback = null;
        private AsyncCallback myReceiveKerberosCallback = null;
        private AsyncCallback mySendSSLCallback = null;
        private AsyncCallback myReceiveSSLCallback = null;

        private Timer _BootUpTimer = null;
        private int _BootUpTimes = 0;

        Packet_Send byteData;

        private bool SendFound = false;
        private bool _CallEmptyBuffer = false;

        private int mode = 0;
        private int size = 0;
        private bool action2 = false;

        private byte[] _timeoutdata = BitConverter.GetBytes((int)(-1));
        private Packet_Send _timeoutpacket = new Packet_Send(BitConverter.GetBytes((int)(-1)), false);

        internal volatile bool _IsAccepted = false;

        /// <summary>
        /// the IPAdress of the remote end point
        /// </summary>
        public IPAddress IPv4Address
        {
            get;
            internal set;
        }

        /// <summary>
        /// Link of the connection
        /// </summary>
        public Link link
        {
            get;
            internal set;
        }

        internal VaserServer server
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(Socket client, bool _IsServer, VaserOptions Mode, PortalCollection PColl, VaserKerberosServer KerberosS, VaserSSLServer SSLS, VaserKerberosClient KerberosC, VaserSSLClient SSLC, VaserServer srv = null)
        {
            IsServer = _IsServer;
            StreamIsConnected = true;

            _rms2 = new MemoryStream();
            _rbr2 = new BinaryReader(_rms2);


            _Mode = Mode;
            _PCollection = PColl;

            _vSSLS = SSLS;
            _vKerberosS = KerberosS;
            _vSSLC = SSLC;
            _vKerberosC = KerberosC;

            _SocketTCPClient = client;
            _SocketTCPClient.LingerState = new LingerOption(true, 0);

            server = srv;

            IPv4Address = ((IPEndPoint)client.RemoteEndPoint).Address;

            link = new Link(PColl)
            {
                Connect = this
            };
            if (Mode == VaserOptions.ModeNotEncrypted)
            {
                mySendNotEncryptedCallback = new AsyncCallback(SendNotEncryptedCallback);
                myReceiveNotEncryptedCallback = new AsyncCallback(ReceiveNotEncryptedCallback);
            }
            if (Mode == VaserOptions.ModeKerberos)
            {
                mySendKerberosCallback = new AsyncCallback(SendKerberosCallback);
                myReceiveKerberosCallback = new AsyncCallback(ReceiveKerberosCallback);
            }

            if (Mode == VaserOptions.ModeSSL)
            {
                mySendSSLCallback = new AsyncCallback(SendSSLCallback);
                myReceiveSSLCallback = new AsyncCallback(ReceiveSSLCallback);
            }
            if (_IsServer)
            {
                ThreadPool.QueueUserWorkItem(HandleClientComm);
            }
            else
            {
                HandleClientComm(null);
            }

        }


        private void _BootUpTimer_Elapsed(object sender)
        {
            _BootUpTimes++;

            //kill the connection attempt after 15 sek

            if (_BootUpTimes > 150)
            {
                _BootUpTimes = 0;
                _BootUpTimer.Dispose();

                try
                {

                    Stop();

                    _rbr2.Dispose();
                    _rbr2 = null;
                    _rms2.Dispose();
                    _buff = null;

                    // encryption
                    /*if (_Mode == VaserOptions.ModeKerberos && _AuthStream != null)
                    {
                        _AuthStream.Close();
                    }

                    if (_Mode == VaserOptions.ModeSSL && _sslStream != null)
                    {
                        _sslStream.Close();
                    }

                    _SocketTCPClient.Close();*/
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Handles the connection process of clients
        /// </summary>
        private void HandleClientComm(object sender)
        {
            //This conntects the client
            //first we need an rescue timer

            _BootUpTimer = new Timer(new TimerCallback(_BootUpTimer_Elapsed), null, 100, 100);

            bool leaveInnerStreamOpen = false;

            try
            {

                // encryption
                if (_Mode == VaserOptions.ModeKerberos)
                {
                    QueueSend = QueueSendKerberos;
                    _AuthStream = new NegotiateStream(new NetworkStream(_SocketTCPClient), leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    QueueSend = QueueSendSSL;
                    _sslStream = new SslStream(new NetworkStream(_SocketTCPClient), leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    QueueSend = QueueSendNotEncrypted;
                }

                if (_Mode == VaserOptions.ModeNamedPipeServerStream)
                {
                    //QueueSend = QueueSendNotEncrypted;
                }

                if (_Mode == VaserOptions.ModeNamedPipeClientStream)
                {
                    //QueueSend = QueueSendNotEncrypted;
                }

                if (IsServer)
                { //server


                    if (_Mode == VaserOptions.ModeKerberos)
                    {



                        if (_vKerberosS._credential == null)
                        {
                            _AuthStream.AuthenticateAsServer();
                        }
                        else
                        {
                            _AuthStream.AuthenticateAsServer(_vKerberosS._credential, _vKerberosS._requiredProtectionLevel, _vKerberosS._requiredImpersonationLevel);
                        }


                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        IIdentity id = _AuthStream.RemoteIdentity;

                        link.UserName = id.Name;
                    }


                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        if (_vSSLS._enabledSslProtocols == SslProtocols.None)
                        {
                            _sslStream.AuthenticateAsServer(_vSSLS._serverCertificate);
                        }
                        else
                        {
                            _sslStream.AuthenticateAsServer(_vSSLS._serverCertificate, _vSSLS._clientCertificateRequired, _vSSLS._enabledSslProtocols, _vSSLS._checkCertificateRevocation);
                        }

                        link.IsEncrypted = true;
                        link.IsServer = true;
                    }

                    if (_Mode == VaserOptions.ModeNotEncrypted)
                    {
                        link.IsServer = true;
                    }

                    if (_Mode == VaserOptions.ModeNamedPipeServerStream)
                    {

                        link.IsServer = true;
                    }

                    link.vServer = server;

                    BootupDone = true;
                    server.AddNewLink(link);


                }
                else
                { //client

                    if (_Mode == VaserOptions.ModeKerberos)
                    {

                        if (_vKerberosC._credential == null)
                        {
                            _AuthStream.AuthenticateAsClient();
                        }
                        else
                        {
                            if (_vKerberosC._requiredProtectionLevel == ProtectionLevel.None && _vKerberosC._requiredImpersonationLevel == TokenImpersonationLevel.None)
                            {
                                _AuthStream.AuthenticateAsClient(_vKerberosC._credential, _vKerberosC._targetName);
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsClient(_vKerberosC._credential, _vKerberosC._targetName, _vKerberosC._requiredProtectionLevel, _vKerberosC._requiredImpersonationLevel);
                            }
                        }


                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        IIdentity id = _AuthStream.RemoteIdentity;

                    }

                    if (_Mode == VaserOptions.ModeSSL)
                    {

                        if (_vSSLC._clientCertificates == null)
                        {
                            _sslStream.AuthenticateAsClient(_vSSLC._targetHost);
                        }
                        else
                        {
                            _sslStream.AuthenticateAsClient(_vSSLC._targetHost, _vSSLC._clientCertificates, _vSSLC._enabledSslProtocols, _vSSLC._checkCertificateRevocation);
                        }


                        link.IsEncrypted = true;
                    }

                    if (_Mode == VaserOptions.ModeNamedPipeClientStream)
                    {


                    }

                    //Thread.Sleep(50);
                    BootupDone = true;

                    _IsAccepted = true;
                    if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.QueueUserWorkItem(ReceiveNotEncrypted);
                    if (_Mode == VaserOptions.ModeKerberos) ThreadPool.QueueUserWorkItem(ReceiveKerberos);
                    if (_Mode == VaserOptions.ModeSSL) ThreadPool.QueueUserWorkItem(ReceiveSSL);
                }



            }
            catch (AuthenticationException e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Dispose();

                Stop();
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Dispose();

                Stop();
                return;
            }
            // encryption END

            _BootUpTimer.Dispose();
            _BootUpTimer = null;
        }

        internal void AcceptConnection()
        {
            if (_IsAccepted == false)
            {
                _IsAccepted = true;
                //new Thread(Receive).Start();
                if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.QueueUserWorkItem(ReceiveNotEncrypted);
                if (_Mode == VaserOptions.ModeKerberos) ThreadPool.QueueUserWorkItem(ReceiveKerberos);
                if (_Mode == VaserOptions.ModeSSL) ThreadPool.QueueUserWorkItem(ReceiveSSL);
            }
        }



        internal void QueueSendNotEncrypted()
        {
            lock (link.SendData_Lock)
            {
                if (IsInSendQueue == false)
                {
                    IsInSendQueue = true;
                    //new Thread(Send).Start();
                    ThreadPool.QueueUserWorkItem(SendNotEncrypted);
                }
            }
        }

        internal void QueueSendKerberos()
        {
            lock (link.SendData_Lock)
            {
                if (IsInSendQueue == false)
                {
                    IsInSendQueue = true;
                    //new Thread(Send).Start();
                    ThreadPool.QueueUserWorkItem(SendKerberos);
                }
            }
        }

        internal void QueueSendSSL()
        {
            lock (link.SendData_Lock)
            {
                if (IsInSendQueue == false)
                {
                    IsInSendQueue = true;
                    //new Thread(Send).Start();
                    ThreadPool.QueueUserWorkItem(SendSSL);
                }
            }
        }

        internal static void QueueOnEmptyBuffer()
        {
            if (IsInOnEmptyBufferQueue == false)
            {
                IsInOnEmptyBufferQueue = true;
                ThreadPool.QueueUserWorkItem(WorkOnEmptyBuffer);
            }
        }


        internal static void WorkOnEmptyBuffer(Object threadContext)
        {
            while (true)
            {
                LinkEventArgs LinkEA = null;
                lock (_CallOnEmptyBuffer_Lock)
                {
                    LinkEA = _CallOnEmptyBufferQueue.Dequeue();
                }

                if (LinkEA.lnk.IsConnected) LinkEA.lnk.OnEmptyBuffer(LinkEA);

                lock (_CallOnEmptyBuffer_Lock)
                {
                    if (_CallOnEmptyBufferQueue.Count == 0)
                    {
                        IsInOnEmptyBufferQueue = false;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Stops the connection
        /// </summary>
        public void Stop()
        {
            link.Dispose();
        }


        private static void DisconnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the disconnect request.
                Socket client = (Socket)ar.AsyncState;
                client.EndDisconnect(ar);
                Debug.WriteLine("Disconnected.");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Connection.DisconnectCallback()  >" + e.ToString());
            }
        }

        internal void Dispose()
        {

            lock (_DisposeLock)
            {
                if (Disposed)
                {
                    return;
                }
                else
                {
                    Disposed = true;
                }
            }
            StreamIsConnected = false;
            if (_SocketTCPClient.Connected) _SocketTCPClient.Shutdown(SocketShutdown.Send);
            //if (_SocketTCPClient.Connected) _SocketTCPClient.Disconnect(true);
        }





        #region Receive


        internal void ReceiveNotEncrypted(Object threadContext)
        {

            try
            {
                _SocketTCPClient.BeginReceive(_buff, 0, _buff.Length, 0, myReceiveNotEncryptedCallback, _SocketTCPClient);
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }

        }

        private SocketError ReceiveErrorCode;
        private void ReceiveNotEncryptedCallback(IAsyncResult iar)
        {
            try
            {
                bytesRead = _SocketTCPClient.EndReceive(iar, out ReceiveErrorCode);
                if (bytesRead != 0 && ReceiveErrorCode == SocketError.Success)
                {
                    WritePackets();
                    _SocketTCPClient.BeginReceive(_buff, 0, _buff.Length, 0, myReceiveNotEncryptedCallback, _SocketTCPClient);
                }
                else
                {
                    StreamIsConnected = false;
                    Stop();

                    _rbr2.Dispose();
                    _rbr2 = null;
                    _rms2.Dispose();
                    _buff = null;

                }
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }
        }

        internal void ReceiveSSL(Object threadContext)
        {
            try
            {
                _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveSSLCallback, _sslStream);
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }
        }

        private void ReceiveSSLCallback(IAsyncResult iar)
        {
            try
            {
                bytesRead = _sslStream.EndRead(iar);
                if (bytesRead != 0)
                {
                    WritePackets();

                    _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveSSLCallback, _sslStream);
                }
                else
                {
                    StreamIsConnected = false;
                    Stop();

                    _rbr2.Dispose();
                    _rbr2 = null;
                    _rms2.Dispose();
                    _buff = null;
                }
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }
        }

        internal void ReceiveKerberos(Object threadContext)
        {
            try
            {
                _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveKerberosCallback, _AuthStream);
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }
        }

        private void ReceiveKerberosCallback(IAsyncResult iar)
        {
            try
            {
                bytesRead = _AuthStream.EndRead(iar);
                if (bytesRead != 0)
                {
                    WritePackets();

                    _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveKerberosCallback, _AuthStream);
                }
                else
                {
                    StreamIsConnected = false;
                    Stop();

                    _rbr2.Dispose();
                    _rbr2 = null;
                    _rms2.Dispose();
                    _buff = null;
                }
            }
            catch
            {
                StreamIsConnected = false;
                Stop();

                _rbr2.Dispose();
                _rbr2 = null;
                _rms2.Dispose();
                _buff = null;
            }
        }


        #endregion

        private List<Packet_Recv> inlist = new List<Packet_Recv>();
        private void WritePackets()
        {
            //Debug.WriteLine("Write in _rms2 " + bytesRead + "  _rms2.Position " + _rms2.Position);
            _rms2.Write(_buff, 0, bytesRead);
            //Debug.WriteLine("New _rms2.Position " + _rms2.Position);
            _rms2.Position = 0;

            action2 = true;
            while (action2)
            {
                action2 = false;
                switch (mode)
                {
                    case 0: // get the packetsize
                        if ((_rms2.Length - _rms2.Position) >= 4)
                        {

                            size = _rbr2.ReadInt32();

                            mode = 1;
                            action2 = true;

                            // recive keep alive packet
                            if (size == -1)
                            {
                                mode = 0;
                                action2 = true;
                            }
                            else
                            {
                                //Debug.WriteLine("size " + size );
                                // if the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?
                                if (size > Options.MaximumPacketSize || size < Options.PacketHeadSize)
                                {
                                    //Debug.WriteLine("The Size was: " + size + " > the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?");
                                    this.Stop();
                                    mode = 100;
                                    break;
                                }
                            }
                        }
                        break;
                    case 1: // recive the packet und give it to the class
                        if ((_rms2.Length - _rms2.Position) >= size)
                        {

                            if (size == Options.PacketHeadSize)
                            {
                                inlist.Add(new Packet_Recv(link, _rbr2));
                                //_PCollection.GivePacketToClass(new Packet_Recv(link, _rbr2));
                            }
                            else
                            {
                                Packet_Recv Recv = new Packet_Recv(link, _rbr2)
                                {
                                    Data = _rbr2.ReadBytes(size - Options.PacketHeadSize)
                                };
                                //_PCollection.GivePacketToClass(Recv);
                                inlist.Add(Recv);
                            }

                            mode = 0;

                            action2 = true;
                        }
                        break;
                }
            }

            _PCollection.GivePacketToClass(inlist);
            inlist.Clear();
            //Debug.WriteLine("last _rms2.Length " + _rms2.Length+ "  _rms2.Position " + _rms2.Position);
            if (_rms2.Length == _rms2.Position)
            {
                _rms2.SetLength(0);
                //_rms2.Flush();
                _rms2.Position = 0;
            }
            else
            {
                byte[] lastbytes = new byte[(int)(_rms2.Length - _rms2.Position)];
                //byte[] lastbytes = _rbr2.ReadBytes((int)(_rms2.Length - _rms2.Position));
                _rms2.Read(lastbytes, 0, (int)(_rms2.Length - _rms2.Position));

                _rms2.SetLength(0);
                //_rms2.Flush();
                _rms2.Position = 0;

                _rms2.Write(lastbytes, 0, lastbytes.Length);
                _rms2.Position = lastbytes.Length;

            }
        }

        #region Send

        // *********************************************************
        // WARNING: if you get an AccessValidation error, check following:
        // - do you try to send data to a connecting or closed stream?
        // - do you try to send data with multiple threads at the same time?
        // - do you try to send and receive data with the same thread?
        // - RTFM! No no no, listen READ THE F MANUAL: https://msdn.microsoft.com/de-de/library/fx6588te%28v=vs.110%29.aspx
        // *********************************************************
        internal void SendNotEncrypted(Object threadContext)
        {
            try
            {
                if (GetPackets()) return;
                if (_SocketTCPClient.Connected)
                {
                    _SocketTCPClient.BeginSend(byteData._SendData, 0, byteData._SendData.Length, 0, mySendNotEncryptedCallback, _SocketTCPClient);
                }
                else
                {
                    Stop();
                }
            }
            catch
            {
                Stop();
            }
        }

        private SocketError SendErrorCode;
        private void SendNotEncryptedCallback(IAsyncResult iar)
        {
            try
            {
                _SocketTCPClient.EndSend(iar, out SendErrorCode);
                byteData._SendData = null;
                if (SendErrorCode == SocketError.Success)
                {
                    SendNotEncrypted(null);
                }
                else
                {
                    Stop();
                }
            }
            catch
            {
                Stop();
            }
        }


        internal void SendKerberos(Object threadContext)
        {
            try
            {
                if (GetPackets()) return;
                if (_SocketTCPClient.Connected)
                {
                    _AuthStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, mySendKerberosCallback, _AuthStream);
                }
                else
                {
                    Stop();
                }
            }
            catch
            {
                Stop();
            }
        }

        private void SendKerberosCallback(IAsyncResult iar)
        {
            try
            {
                _AuthStream.EndWrite(iar);
                byteData._SendData = null;
                SendKerberos(null);
            }
            catch
            {
                Stop();
            }
        }

        internal void SendSSL(Object threadContext)
        {
            try
            {
                if (GetPackets()) return;
                if (_SocketTCPClient.Connected)
                {
                    _sslStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, mySendSSLCallback, _sslStream);
                }
                else
                {
                    Stop();
                }
            }
            catch
            {
                Stop();
            }
        }

        private void SendSSLCallback(IAsyncResult iar)
        {
            try
            {
                _sslStream.EndWrite(iar);
                byteData._SendData = null;
                SendSSL(null);
            }
            catch
            {
                Stop();
            }
        }

        #endregion


        private bool GetPackets()
        {
            SendFound = false;

            lock (link.SendData_Lock)
            {
                if (link.SendDataPortalArrayOUTPUT[0] == null)
                {
                    return true;
                }
                for (int x = 0; x < link.SendDataPortalArrayOUTPUT.Length; x++)
                {
                    if (link.SendDataPortalArrayOUTPUT[x].Count > 0)
                    {
                        byteData = link.SendDataPortalArrayOUTPUT[x].Dequeue();
                        SendFound = true;
                        if (byteData._CallEmpybuffer) _CallEmptyBuffer = true;
                        break;
                    }
                }
                if (!SendFound)
                {
                    IsInSendQueue = false;
                    //Debug.WriteLine("no data");
                    //if _CallEmptyBuffer is set, trigger an event to get more data
                    if (_CallEmptyBuffer)
                    {
                        _CallEmptyBuffer = false;

                        LinkEventArgs args = new LinkEventArgs()
                        {
                            lnk = link
                        };
                        lock (_CallOnEmptyBuffer_Lock)
                        {
                            _CallOnEmptyBufferQueue.Enqueue(args);
                        }

                        QueueOnEmptyBuffer();
                    }


                    return true;
                }
            }

            return false;
        }



    }
}
