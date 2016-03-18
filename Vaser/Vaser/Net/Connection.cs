using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Timers;

namespace Vaser
{
    internal class Connection
    {

        private object _Settings_ThreadLock = new object();
        private object _DisposeLock = new object();
        public volatile bool ThreadIsRunning = true;
        public volatile bool StreamIsConnected = true;
        public volatile bool IsServer = false;
        private NetworkStream _ConnectionStream;
        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        private NetworkStream _NotEncryptedStream;
        //private Thread _ProcessingDecryptThread;
        //private Thread _ClientThread;
        private TcpClient _SocketTCPClient;
        public volatile bool Disposed;
        public volatile bool BootupDone = false;

        private PortalCollection _PCollection = null;

        private int bytesRead;

        private byte[] _buff = new byte[65012];

        private VaserServer _server;

        private Link _link;
        private IPAddress _IPv4Address;

        private object _WorkAtStream_Lock = new object();

        public static volatile bool IsInOnEmptyBufferQueue;
        private static object _CallOnEmptyBuffer_Lock = new object();
        private static Queue<LinkEventArgs> _CallOnEmptyBufferQueue = new Queue<LinkEventArgs>();

        private object _ReadStream_Lock = new object();
        private MemoryStream _rms1 = null;
        private BinaryWriter _rbw1 = null;
        private BinaryReader _rbr1 = null;

        private MemoryStream _rms2 = null;
        private BinaryWriter _rbw2 = null;
        private BinaryReader _rbr2 = null;

        private VaserOptions _Mode = null;
        //private X509Certificate2 _Cert = null;
        //private X509Certificate2Collection _CertCol = null;
        //private string _targetHostname = string.Empty;
        private VaserSSLServer _vSSLS = null;
        private VaserKerberosServer _vKerberosS = null;
        private VaserSSLClient _vSSLC = null;
        private VaserKerberosClient _vKerberosC = null;

        private volatile bool IsInQueue = false;
        private volatile bool IsInSendQueue = false;

        private System.Timers.Timer _aTimer;

        internal volatile bool _IsAccepted = false;

        /// <summary>
        /// the IPAdress of the remote end point
        /// </summary>
        public IPAddress IPv4Address
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _IPv4Address;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _IPv4Address = value;
                }
            }
        }

        /// <summary>
        /// Link of the connection
        /// </summary>
        public Link link
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _link;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _link = value;
                }
            }
        }

        internal VaserServer server
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _server;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _server = value;
                }
            }
        }

        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(TcpClient client, bool _IsServer, VaserOptions Mode, PortalCollection PColl, VaserKerberosServer KerberosS, VaserSSLServer SSLS, VaserKerberosClient KerberosC, VaserSSLClient SSLC, VaserServer srv = null)
        {
            IsServer = _IsServer;


            lock (_ReadStream_Lock)
            {
                _rms1 = new MemoryStream();
                _rbw1 = new BinaryWriter(_rms1);
                _rbr1 = new BinaryReader(_rms1);

                _rms2 = new MemoryStream();
                _rbw2 = new BinaryWriter(_rms2);
                _rbr2 = new BinaryReader(_rms2);

            }

            _aTimer = new System.Timers.Timer(5000);
            _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            _Mode = Mode;
            _PCollection = PColl;

            //_Cert = Cert;
            _vSSLS = SSLS;
            _vKerberosS = KerberosS;
            _vSSLC = SSLC;
            _vKerberosC = KerberosC;
            //_targetHostname = targetHostname;

            _SocketTCPClient = client;

            server = srv;

            // encryption
            //_SocketTCPClient.LingerState = (new LingerOption(true, 2));
            // encryption END


            IPv4Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            link = new Link(PColl);
            link.Connect = this;

            myCallBack = new AsyncCallback(SendCallback);
            myReceiveCallback = new AsyncCallback(ReceiveCallback);

            HandleClientComm();


        }

        private System.Timers.Timer _BootUpTimer = null;
        private int _BootUpTimes = 0;
        private void _BootUpTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _BootUpTimes++;

            //kill the connection attempt after 15 sek

            if (_BootUpTimes > 150)
            {
                _BootUpTimes = 0;
                _BootUpTimer.Stop();

                try
                {
                    _SocketTCPClient.Close();

                    _ConnectionStream.Close();
                    _ConnectionStream.Dispose();

                    // encryption
                    if (_Mode == VaserOptions.ModeKerberos && _AuthStream != null)
                    {
                        _AuthStream.Close();
                        _AuthStream.Dispose();
                    }

                    if (_Mode == VaserOptions.ModeSSL && _sslStream != null)
                    {
                        _sslStream.Close();
                        _sslStream.Dispose();
                    }

                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Handles the connection process of clients
        /// </summary>
        private void HandleClientComm()
        {
            //This conntects the client
            //first we need an rescue timer

            _BootUpTimer = new System.Timers.Timer(100);
            _BootUpTimer.Enabled = true;
            _BootUpTimer.Elapsed += _BootUpTimer_Elapsed;
            _BootUpTimer.Start();

            bool leaveInnerStreamOpen = false;

            try
            {

                _ConnectionStream = _SocketTCPClient.GetStream();


                // encryption
                if (_Mode == VaserOptions.ModeKerberos)
                {
                    _AuthStream = new NegotiateStream(_ConnectionStream, leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    _sslStream = new SslStream(_ConnectionStream, leaveInnerStreamOpen);
                }

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    _NotEncryptedStream = _ConnectionStream;
                }

                if (IsServer)
                { //server


                    if (_Mode == VaserOptions.ModeKerberos)
                    {


                        if (_vKerberosS._policy == null)
                        {
                            if (_vKerberosS._credential == null)
                            {
                                _AuthStream.AuthenticateAsServer();
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsServer(_vKerberosS._credential, _vKerberosS._requiredProtectionLevel, _vKerberosS._requiredImpersonationLevel);
                            }
                        }
                        else
                        {
                            if (_vKerberosS._credential == null)
                            {
                                _AuthStream.AuthenticateAsServer(_vKerberosS._policy);
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsServer(_vKerberosS._credential, _vKerberosS._policy, _vKerberosS._requiredProtectionLevel, _vKerberosS._requiredImpersonationLevel);
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
                        }
                        else
                        {
                            if (_vKerberosC._requiredProtectionLevel == ProtectionLevel.None && _vKerberosC._requiredImpersonationLevel == TokenImpersonationLevel.None)
                            {
                                _AuthStream.AuthenticateAsClient(_vKerberosC._credential, _vKerberosC._binding, _vKerberosC._targetName);
                            }
                            else
                            {
                                _AuthStream.AuthenticateAsClient(_vKerberosC._credential, _vKerberosC._binding, _vKerberosC._targetName, _vKerberosC._requiredProtectionLevel, _vKerberosC._requiredImpersonationLevel);
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

                    //Thread.Sleep(50);
                    BootupDone = true;

                    _IsAccepted = true;
                    Receive();
                }



                _aTimer.Enabled = true;
                _aTimer.Start();

            }
            catch (AuthenticationException e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Stop();

                Dispose();
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Stop();

                Dispose();
                return;
            }
            // encryption END

            _BootUpTimer.Stop();
            _BootUpTimer.Dispose();
            _BootUpTimer = null;
        }

        internal void AcceptConnection()
        {
            if (_IsAccepted == false)
            {
                _IsAccepted = true;
                Receive();
            }
        }

        private byte[] _timeoutdata = BitConverter.GetBytes((int)(-1));
        private Packet_Send _timeoutpacket = new Packet_Send(BitConverter.GetBytes((int)(-1)), false);
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //Debug.WriteLine("Send keep alive packet {0}", e.SignalTime);
            lock (_link.SendData_Lock)
            {
                if (_link.SendDataPortalArrayOUTPUT[0] != null) _link.SendDataPortalArrayOUTPUT[0].Enqueue(_timeoutpacket);
            }
            SendData();
        }


        internal void QueueStreamDecrypt()
        {
            if (IsInQueue == false)
            {
                IsInQueue = true;
                ThreadPool.QueueUserWorkItem(ThreadPoolCallback);
            }
        }

        internal void QueueSend()
        {
            if (IsInSendQueue == false)
            {
                IsInSendQueue = true;
                ThreadPool.QueueUserWorkItem(Send);
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

        // Wrapper method for use with thread pool.
        internal void ThreadPoolCallback(Object threadContext)
        {

            StreamDecrypt();
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="Data"></param>
        internal void SendData()
        {

            if (StreamIsConnected)
            {

                QueueSend();
            }


        }



        /// <summary>
        /// Stops the connection
        /// </summary>
        public void Stop()
        {
            ThreadIsRunning = false;
            Dispose();
        }

        private int mode = 0;
        private int size = 0;
        private bool action1 = false;
        private bool action2 = false;

        private void StreamDecrypt()
        {

            try
            {
                lock (_WorkAtStream_Lock)
                {


                    if (_rms1 == null)
                    {
                        return;
                    }

                    action1 = true;


                    while (action1)
                    {
                        action1 = false;
                        //_ReadStream_Lock.Wait();
                        lock (_ReadStream_Lock)
                        {
                            if (_rms1.Length > 0) action1 = true;
                            //Debug.WriteLine("Decrypting: _rms1.Length = " + _rms1.Length);
                            _rbw2.Write(_rms1.ToArray());

                            _rms1.SetLength(0);
                            _rms1.Flush();
                            _rbw1.Flush();

                            IsInQueue = false;
                        }
                        //_ReadStream_Lock.Release();
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

                                            // if the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?
                                            if (size > Options.MaximumPacketSize || size < Options.PacketHeadSize)
                                            {
                                                Debug.WriteLine("The Size was: " + size + " > the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?");
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

                                        if (size - Options.PacketHeadSize == 0)
                                        {
                                            _PCollection.GivePacketToClass(new Packet_Recv(link, _rbr2), null);
                                        }
                                        else
                                        {
                                            _PCollection.GivePacketToClass(new Packet_Recv(link, _rbr2), _rbr2.ReadBytes(size - Options.PacketHeadSize));
                                        }

                                        mode = 0;

                                        action2 = true;
                                    }
                                    break;
                            }
                        }


                        byte[] lastbytes = _rbr2.ReadBytes((int)(_rms2.Length - _rms2.Position));


                        _rms2.SetLength(0);
                        _rms2.Flush();
                        _rbw2.Flush();

                        _rbw2.Write(lastbytes);

                    }

                }

            }
            catch (Exception e)
            {


                Debug.WriteLine("Connection.StreamDecrypt()  >" + e.ToString());
                //Dispose();
                ThreadIsRunning = false;
            }

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
                //Debug.WriteLine("Link.Dispose called");
                if (Disposed)
                {
                    //Debug.WriteLine("Link.Dispose abort");
                    return;
                }
                else
                {
                    Disposed = true;
                }
                StreamIsConnected = false;
                ThreadIsRunning = false;

                if (_SocketTCPClient != null && _SocketTCPClient.Connected)
                {
                    try
                    {
                        _SocketTCPClient.Client.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {

                    }

                }



                //Thread.Sleep(10);


                if (_AuthStream != null) _AuthStream.Close();
                if (_sslStream != null) _sslStream.Close();
                _ConnectionStream.Close();
                _SocketTCPClient.Close();

                if (_AuthStream != null) _AuthStream.Dispose();
                if (_sslStream != null) _sslStream.Dispose();
                _ConnectionStream.Dispose();

                _AuthStream = null;
                _sslStream = null;
                _ConnectionStream = null;
                _SocketTCPClient = null;


                _aTimer.Stop();
                _aTimer.Dispose();
                _aTimer = null;

                lock (_WorkAtStream_Lock)
                {

                    lock (_ReadStream_Lock)
                    {
                        try
                        {

                            _rbr1.Dispose();
                            _rbr2.Dispose();

                            _rbr1 = null;
                            _rbr2 = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                        }

                        try
                        {
                            _rbw1.Dispose();
                            _rbw2.Dispose();

                            _rbw1 = null;
                            _rbw2 = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                        }

                        try
                        {
                            _rms1.Dispose();
                            _rms2.Dispose();

                            _rms1 = null;
                            _rms2 = null;

                            myCallBack = null;
                            myReceiveCallback = null;

                            _buff = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                        }


                    }
                }

                if (server != null) server.RemoveFromConnectionList(this);

                if (link != null)
                {
                    link.Dispose();
                    link = null;
                }

                Debug.WriteLine("Link.Dispose finished");
            }
        }


        Packet_Send byteData = null;
        internal void Receive()
        {
            try
            {
                //Console.WriteLine("Stream ReadLength b4 read" );
                if (!StreamIsConnected) return;

                // Begin receiving the data from the remote device.
                if (_AuthStream != null)
                {
                    _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _AuthStream);
                }
                if (_sslStream != null)
                {
                    _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _sslStream);
                }
                if (_NotEncryptedStream != null)
                {
                    _NotEncryptedStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _NotEncryptedStream);
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Dispose();

                Debug.WriteLine("Connection.Receive()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        private void ReceiveCallback(IAsyncResult iar)
        {
            if (!BootupDone) throw new Exception("Data was recived b4 connection was booted.");
            try
            {

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    NetworkStream sendingSocket = (NetworkStream)iar.AsyncState;
                    bytesRead = sendingSocket.EndRead(iar);
                }

                if (_Mode == VaserOptions.ModeKerberos)
                {
                    NegotiateStream sendingSocket = (NegotiateStream)iar.AsyncState;
                    bytesRead = sendingSocket.EndRead(iar);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    SslStream sendingSocket = (SslStream)iar.AsyncState;
                    bytesRead = sendingSocket.EndRead(iar);
                }

                // Read data from the remote device.
                /*if (_AuthStream != null)
                {
                    bytesRead = _AuthStream.EndRead(ar);
                }
                if (_sslStream != null)
                {
                    bytesRead = _sslStream.EndRead(ar);
                }
                if (_NotEncryptedStream != null)
                {
                    bytesRead = _NotEncryptedStream.EndRead(ar);
                }*/


                if (bytesRead > 0)
                {
                    //Debug.WriteLine("{0} bytes read by the Connection.", bytesRead);

                    lock (_ReadStream_Lock)
                    {
                        _rbw1.Write(_buff, 0, bytesRead);

                        QueueStreamDecrypt();
                    }

                }

                if (!StreamIsConnected) return;
                // Contiue Receive...
                //Receive();

                // Get the rest of the data.
                if (_AuthStream != null && _AuthStream.CanRead)
                {
                    _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _AuthStream);
                }
                if (_sslStream != null && _sslStream.CanRead)
                {
                    _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _sslStream);
                }

                if (_NotEncryptedStream != null && _NotEncryptedStream.CanRead)
                {
                    _NotEncryptedStream.BeginRead(_buff, 0, _buff.Length, myReceiveCallback, _NotEncryptedStream);
                }

            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                //_AuthStream = null;
                //_sslStream = null;

                Dispose();

                Debug.WriteLine("Connection.ReceiveCallback()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }




        bool SendFound = false;
        bool _CallEmptyBuffer = false;
        internal AsyncCallback myCallBack = null;
        internal AsyncCallback myReceiveCallback = null;

        // *********************************************************
        // WARNING: if you get an AccessValidation error, check following:
        // - do you try to send data to a connecting or closed stream?
        // - do you try to send data with multiple threads at the same time?
        // - do you try to send and receive data with the same thread?
        // - RTFM! No no no, listen READ THE F MANUAL: https://msdn.microsoft.com/de-de/library/fx6588te%28v=vs.110%29.aspx
        // *********************************************************
        internal void Send(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {
                if (!StreamIsConnected) return;

                //Debug.WriteLine("attempt to send data");

                //Debug.WriteLine("locked");
                SendFound = false;
                lock (_link.SendData_Lock)
                {
                    for (int x = 0; x < _link.SendDataPortalArrayOUTPUT.Length; x++)
                    {
                        if (_link.SendDataPortalArrayOUTPUT[x].Count > 0)
                        {

                            //Debug.WriteLine("data");
                            byteData = _link.SendDataPortalArrayOUTPUT[x].Dequeue();
                            SendFound = true;
                            if (byteData._CallEmpybuffer) _CallEmptyBuffer = true;

                            //Debug.WriteLine("Sending.... Lenght: " + byteData._SendData.Length);
                            break;
                        }
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

                        LinkEventArgs args = new LinkEventArgs();
                        args.lnk = link;


                        lock (_CallOnEmptyBuffer_Lock)
                        {
                            _CallOnEmptyBufferQueue.Enqueue(args);
                        }

                        QueueOnEmptyBuffer();

                    }


                    return;

                }



                if (!StreamIsConnected) return;

                if (_AuthStream != null && _AuthStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _AuthStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, myCallBack, _AuthStream);
                }
                if (_sslStream != null && _sslStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _sslStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, myCallBack, _sslStream);
                }

                if (_NotEncryptedStream != null && _NotEncryptedStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _NotEncryptedStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, myCallBack, _NotEncryptedStream);
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Dispose();

                Debug.WriteLine("Connection.Send()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        private void SendCallback(IAsyncResult iar)
        {

            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {
                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    NetworkStream sendingSocket = (NetworkStream)iar.AsyncState;
                    sendingSocket.EndWrite(iar);
                }

                if (_Mode == VaserOptions.ModeKerberos)
                {
                    NegotiateStream sendingSocket = (NegotiateStream)iar.AsyncState;
                    sendingSocket.EndWrite(iar);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    SslStream sendingSocket = (SslStream)iar.AsyncState;
                    sendingSocket.EndWrite(iar);
                }

                // Complete sending the data to the remote device.
                /*if (_AuthStream != null) _AuthStream.EndWrite(ar);
                if (_sslStream != null) _sslStream.EndWrite(ar);
                if (_NotEncryptedStream != null) _NotEncryptedStream.EndWrite(ar);*/

                // Contiue sending...
                Send(null);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Connection.SendCallback()  > " + e.ToString());

                StreamIsConnected = false;
                Dispose();
            }
        }
    }

}
