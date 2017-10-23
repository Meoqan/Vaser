using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;
using Vaser.ConnectionSettings;

namespace Vaser
{
    internal class Connection
    {
        private object _DisposeLock = new object();
        public volatile bool IsServer = false;

        private NetworkStream _ConnectionStream;
        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        //private NetworkStream _NotEncryptedStream;


        public bool StreamIsConnected
        {
            get; set;
        }

        int MaximumPacketSize = Options.MaximumPacketSize;
        int PacketHeadSize = Options.PacketHeadSize;
        bool EnableHeartbeat = Options.EnableHeartbeat;
        int HeartbeatMilliseconds = Options.HeartbeatMilliseconds; // 60 sec

        private Socket _SocketTCPClient;
        public bool Disposed;
        public volatile bool BootupDone = false;

        internal delegate void QueueSendHidden();
        internal QueueSendHidden QueueSend = null;

        internal PortalCollection _PCollection = null;

        private int bytesRead;

        private byte[] _buff = new byte[65012];

        //private VaserServer _server;

        //private Link _link;
        //private IPAddress _IPv4Address;
        
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
        
        private Timer _BootUpTimer = null;
        private int _BootUpTimes = 0;

        private byte[] _timeoutdata = BitConverter.GetBytes((int)(-1));
        private Packet_Send _timeoutpacket = new Packet_Send(BitConverter.GetBytes((int)(-1)), false);
        private Timer HeartbeatTimer = null;

        Packet_Send byteData;

        bool SendFound = false;
        bool _CallEmptyBuffer = false;

        private int mode = 0;
        private int size = 0;
        private bool action2 = false;

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

            client.SendBufferSize = 65007;
            _SocketTCPClient = client;

            server = srv;
            
            IPv4Address = ((IPEndPoint)client.RemoteEndPoint).Address;

            link = new Link(PColl);
            link.Connect = this;

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

                    if (_rbr2 != null) _rbr2.Dispose();
                    if (_rbr2 != null) _rbr2 = null;
                    if (_rms2 != null) _rms2.Dispose();
                    if (_buff != null) _buff = null;

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

        private void OnHeartbeatEvent(object source)
        {
            //Debug.WriteLine("Send Heartbeat packet. "+DateTime.Now);
            lock (link.SendData_Lock)
            {
                if (link.SendDataPortalArrayOUTPUT[0] != null) link.SendDataPortalArrayOUTPUT[0].Enqueue(_timeoutpacket);
            }
            QueueSend();
        }

        /// <summary>
        /// Handles the connection process of clients
        /// </summary>
        private void HandleClientComm(object sender)
        {
            //This conntects the client
            //first we need an rescue timer

            _BootUpTimer = new Timer(new TimerCallback(_BootUpTimer_Elapsed), null, 0, 100);

            bool leaveInnerStreamOpen = false;

            try
            {

                _SocketTCPClient.LingerState = new LingerOption(true, 10);

                // encryption
                if (_Mode == VaserOptions.ModeKerberos)
                {
                    _ConnectionStream = new NetworkStream(_SocketTCPClient);
                    QueueSend = QueueSendKerberos;
                    _AuthStream = new NegotiateStream(_ConnectionStream, leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    _ConnectionStream = new NetworkStream(_SocketTCPClient);
                    QueueSend = QueueSendSSL;
                    _sslStream = new SslStream(_ConnectionStream, leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    QueueSend = QueueSendNotEncrypted;
                    //_NotEncryptedStream = _ConnectionStream;
                }

                if (IsServer)
                { //server


                    if (_Mode == VaserOptions.ModeKerberos)
                    {


                        if (_vKerberosS._policy == null)
                        {
                            if (_vKerberosS._credential == null)
                            {
                                _AuthStream.AuthenticateAsServerAsync();
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsServerAsync(_vKerberosS._credential, _vKerberosS._requiredProtectionLevel, _vKerberosS._requiredImpersonationLevel);
                            }
                        }
                        else
                        {
                            if (_vKerberosS._credential == null)
                            {
                                _AuthStream.AuthenticateAsServerAsync(_vKerberosS._policy);
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsServerAsync(_vKerberosS._credential, _vKerberosS._policy, _vKerberosS._requiredProtectionLevel, _vKerberosS._requiredImpersonationLevel);
                            }
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
                            _sslStream.AuthenticateAsServerAsync(_vSSLS._serverCertificate);
                        }
                        else
                        {
                            _sslStream.AuthenticateAsServerAsync(_vSSLS._serverCertificate, _vSSLS._clientCertificateRequired, _vSSLS._enabledSslProtocols, _vSSLS._checkCertificateRevocation);
                        }

                        link.IsEncrypted = true;
                        link.IsServer = true;
                    }

                    if (_Mode == VaserOptions.ModeNotEncrypted)
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
                        if (_vKerberosC._binding == null)
                        {
                            if (_vKerberosC._credential == null)
                            {
                                _AuthStream.AuthenticateAsClientAsync();
                            }
                            else
                            {
                                if (_vKerberosC._requiredProtectionLevel == ProtectionLevel.None && _vKerberosC._requiredImpersonationLevel == TokenImpersonationLevel.None)
                                {
                                    _AuthStream.AuthenticateAsClientAsync(_vKerberosC._credential, _vKerberosC._targetName);
                                }
                                else
                                {
                                    _AuthStream.AuthenticateAsClientAsync(_vKerberosC._credential, _vKerberosC._targetName, _vKerberosC._requiredProtectionLevel, _vKerberosC._requiredImpersonationLevel);
                                }
                            }
                        }
                        else
                        {
                            if (_vKerberosC._requiredProtectionLevel == ProtectionLevel.None && _vKerberosC._requiredImpersonationLevel == TokenImpersonationLevel.None)
                            {
                                _AuthStream.AuthenticateAsClientAsync(_vKerberosC._credential, _vKerberosC._binding, _vKerberosC._targetName);
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsClientAsync(_vKerberosC._credential, _vKerberosC._binding, _vKerberosC._targetName, _vKerberosC._requiredProtectionLevel, _vKerberosC._requiredImpersonationLevel);
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
                            _sslStream.AuthenticateAsClientAsync(_vSSLC._targetHost);
                        }
                        else
                        {
                            _sslStream.AuthenticateAsClientAsync(_vSSLC._targetHost, _vSSLC._clientCertificates, _vSSLC._enabledSslProtocols, _vSSLC._checkCertificateRevocation);
                        }


                        link.IsEncrypted = true;
                    }

                    //Thread.Sleep(50);
                    BootupDone = true;

                    _IsAccepted = true;
                    if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.QueueUserWorkItem(ReceiveNotEncrypted);
                    if (_Mode == VaserOptions.ModeKerberos) ThreadPool.QueueUserWorkItem(ReceiveKerberos);
                    if (_Mode == VaserOptions.ModeSSL) ThreadPool.QueueUserWorkItem(ReceiveSSL);
                }

                if (EnableHeartbeat)
                {
                    HeartbeatTimer = new Timer(new TimerCallback(OnHeartbeatEvent), null, HeartbeatMilliseconds, HeartbeatMilliseconds);
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
            try
            {
                if (HeartbeatTimer != null)
                {
                    HeartbeatTimer.Dispose();
                    HeartbeatTimer = null;
                }

                if (_SocketTCPClient.Connected) _SocketTCPClient.Shutdown(SocketShutdown.Send);
            }
            catch
            {
                //error
            }
            //if (_SocketTCPClient.Connected) _SocketTCPClient.Disconnect(true);
        }

        private ArraySegment<byte> _SAbuff = new ArraySegment<byte>(new byte[65007]);
        internal async void ReceiveNotEncrypted(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await _SocketTCPClient.ReceiveAsync(_SAbuff, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        Stop();

                        if (_rbr2 != null) _rbr2.Dispose();
                        if (_rbr2 != null) _rbr2 = null;
                        if (_rms2 != null) _rms2.Dispose();
                        if (_buff != null) _buff = null;

                        return;
                    }
                    _buff = _SAbuff.Array;
                    WritePackets();
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                if (_rbr2 != null) _rbr2.Dispose();
                if (_rbr2 != null) _rbr2 = null;
                if (_rms2 != null) _rms2.Dispose();
                if (_buff != null) _buff = null;

                Debug.WriteLine("Connection.Receive()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        internal async void ReceiveSSL(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await _sslStream.ReadAsync(_buff, 0, _buff.Length);
                    if (bytesRead == 0)
                    {
                        Stop();

                        if (_rbr2 != null) _rbr2.Dispose();
                        if (_rbr2 != null) _rbr2 = null;
                        if (_rms2 != null) _rms2.Dispose();
                        if (_buff != null) _buff = null;

                        return;
                    }
                    WritePackets();
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                if (_rbr2 != null) _rbr2.Dispose();
                if (_rbr2 != null) _rbr2 = null;
                if (_rms2 != null) _rms2.Dispose();
                if (_buff != null) _buff = null;

                Debug.WriteLine("Connection.Receive()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        internal async void ReceiveKerberos(object state)
        {
            //Console.WriteLine("Receive");
            try
            {
                while (true)
                {
                    
                    bytesRead = await _AuthStream.ReadAsync(_buff, 0, _buff.Length);
                    if (bytesRead == 0)
                    {
                        Stop();

                        if (_rbr2 != null) _rbr2.Dispose();
                        if (_rbr2 != null) _rbr2 = null;
                        if (_rms2 != null) _rms2.Dispose();
                        if (_buff != null) _buff = null;

                        return;
                    }
                    WritePackets();
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                if (_rbr2 != null) _rbr2.Dispose();
                if (_rbr2 != null) _rbr2 = null;
                if (_rms2 != null) _rms2.Dispose();
                if (_buff != null) _buff = null;

                Debug.WriteLine("Connection.Receive()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

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

                            // Receive Heartbeat packet
                            if (size == -1)
                            {
                                mode = 0;
                                action2 = true;
                            }
                            else
                            {
                                //Debug.WriteLine("size " + size );
                                // if the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?
                                if (size > MaximumPacketSize || size < PacketHeadSize)
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

                            if (size == PacketHeadSize)
                            {
                                inlist.Add(new Packet_Recv(link, _rbr2));
                                //_PCollection.GivePacketToClass(new Packet_Recv(link, _rbr2));
                            }
                            else
                            {
                                Packet_Recv Recv = new Packet_Recv(link, _rbr2)
                                {
                                    Data = _rbr2.ReadBytes(size - PacketHeadSize)
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

        // *********************************************************
        // WARNING: if you get an AccessValidation error, check following:
        // - do you try to send data to a connecting or closed stream?
        // - do you try to send data with multiple threads at the same time?
        // - do you try to send and receive data with the same thread?
        // - RTFM! No no no, listen READ THE F MANUAL: https://msdn.microsoft.com/de-de/library/fx6588te%28v=vs.110%29.aspx
        // *********************************************************
        internal async void SendNotEncrypted(Object threadContext)
        {
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets()) return;

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient.Connected)
                    {
                        ArraySegment<byte> SA = new ArraySegment<byte>(byteData._SendData);
                        await _SocketTCPClient.SendAsync(SA,SocketFlags.None);
                    }


                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                Debug.WriteLine("Connection.Send()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        internal async void SendKerberos(Object threadContext)
        {
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets()) return;

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient.Connected)
                    {
                        try
                        {
                            await _AuthStream.WriteAsync(byteData._SendData, 0, byteData._SendData.Length);
                        }
                        catch
                        {
                            Stop();
                        }
                    }

                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                Debug.WriteLine("Connection.Send()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        internal async void SendSSL(Object threadContext)
        {
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets()) return;

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient.Connected)
                    {
                        await _sslStream.WriteAsync(byteData._SendData, 0, byteData._SendData.Length);
                    }
                    

                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Stop();

                Debug.WriteLine("Connection.Send()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }


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
