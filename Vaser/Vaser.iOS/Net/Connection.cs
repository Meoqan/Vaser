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
        private object _DisposeLock = new object();
        public volatile bool ThreadIsRunning = true;
        public volatile bool StreamIsConnected = true;
        public volatile bool IsServer = false;

        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        private Socket _SocketTCPClient;

        public volatile bool Disposed;
        public volatile bool BootupDone = false;

        internal delegate void QueueSendHidden();
        internal QueueSendHidden QueueSend = null;

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

        private VaserSSLServer _vSSLS = null;
        private VaserKerberosServer _vKerberosS = null;
        private VaserSSLClient _vSSLC = null;
        private VaserKerberosClient _vKerberosC = null;

        private volatile bool IsInQueue = false;
        private volatile bool IsInSendQueue = false;

        private System.Timers.Timer _aTimer;

        private AsyncCallback mySendNotEncryptedCallback = null;
        private AsyncCallback myReceiveNotEncryptedCallback = null;
        private AsyncCallback mySendKerberosCallback = null;
        private AsyncCallback myReceiveKerberosCallback = null;
        private AsyncCallback mySendSSLCallback = null;
        private AsyncCallback myReceiveSSLCallback = null;

        private System.Timers.Timer _BootUpTimer = null;
        private int _BootUpTimes = 0;

        Packet_Send byteData = null;
        volatile bool _DoDispose = false;

        private bool SendFound = false;
        private bool _CallEmptyBuffer = false;
        private object _SendDisposelock = new object();
        private object _ReceiveDisposelock = new object();

        private int mode = 0;
        private int size = 0;
        private bool action1 = false;
        private bool action2 = false;

        private byte[] _timeoutdata = BitConverter.GetBytes((int)(-1));
        private Packet_Send _timeoutpacket = new Packet_Send(BitConverter.GetBytes((int)(-1)), false);

        internal volatile bool _IsAccepted = false;

        /// <summary>
        /// the IPAdress of the remote end point
        /// </summary>
        public IPAddress IPv4Address
        {
            get
            {
                return _IPv4Address;
            }
            set
            {
                _IPv4Address = value;
            }
        }

        /// <summary>
        /// Link of the connection
        /// </summary>
        public Link link
        {
            get
            {
                return _link;
            }
            set
            {
                _link = value;
            }
        }

        internal VaserServer server
        {
            get
            {
                return _server;
            }
            set
            {
                _server = value;
            }
        }

        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(Socket client, bool _IsServer, VaserOptions Mode, PortalCollection PColl, VaserKerberosServer KerberosS, VaserSSLServer SSLS, VaserKerberosClient KerberosC, VaserSSLClient SSLC, VaserServer srv = null)
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

            mySendNotEncryptedCallback = new AsyncCallback(SendNotEncryptedCallback);
            myReceiveNotEncryptedCallback = new AsyncCallback(ReceiveNotEncryptedCallback);

            mySendKerberosCallback = new AsyncCallback(SendKerberosCallback);
            myReceiveKerberosCallback = new AsyncCallback(ReceiveKerberosCallback);

            mySendSSLCallback = new AsyncCallback(SendSSLCallback);
            myReceiveSSLCallback = new AsyncCallback(ReceiveSSLCallback);

            if (_IsServer)
            {
                ThreadPool.QueueUserWorkItem(HandleClientComm);
            }
            else
            {
                HandleClientComm(null);
            }

        }


        private void _BootUpTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _BootUpTimes++;

            //kill the connection attempt after 15 sek

            if (_BootUpTimes > 150)
            {
                _BootUpTimes = 0;
                _BootUpTimer.Stop();

                lock (_ReceiveDisposelock)
                {
                    lock (_SendDisposelock)
                    {
                        try
                        {
                            _SocketTCPClient.Close();

                            // encryption
                            if (_Mode == VaserOptions.ModeKerberos && _AuthStream != null)
                            {
                                _AuthStream.Close();
                            }

                            if (_Mode == VaserOptions.ModeSSL && _sslStream != null)
                            {
                                _sslStream.Close();
                            }

                        }
                        catch
                        {

                        }
                    }
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

            _BootUpTimer = new System.Timers.Timer(100);
            _BootUpTimer.Enabled = true;
            _BootUpTimer.Elapsed += _BootUpTimer_Elapsed;
            _BootUpTimer.Start();

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

                    //Thread.Sleep(50);
                    BootupDone = true;

                    _IsAccepted = true;
                    if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.QueueUserWorkItem(ReceiveNotEncrypted);
                    if (_Mode == VaserOptions.ModeKerberos) ThreadPool.QueueUserWorkItem(ReceiveKerberos);
                    if (_Mode == VaserOptions.ModeSSL) ThreadPool.QueueUserWorkItem(ReceiveSSL);
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
                //new Thread(Receive).Start();
                if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.QueueUserWorkItem(ReceiveNotEncrypted);
                if (_Mode == VaserOptions.ModeKerberos) ThreadPool.QueueUserWorkItem(ReceiveKerberos);
                if (_Mode == VaserOptions.ModeSSL) ThreadPool.QueueUserWorkItem(ReceiveSSL);
            }
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //Debug.WriteLine("Send keep alive packet {0}", e.SignalTime);
            lock (_link.SendData_Lock)
            {
                if (_link.SendDataPortalArrayOUTPUT[0] != null) _link.SendDataPortalArrayOUTPUT[0].Enqueue(_timeoutpacket);
            }
            QueueSend();
        }


        internal void QueueStreamDecrypt()
        {
            if (IsInQueue == false)
            {
                IsInQueue = true;
                ThreadPool.QueueUserWorkItem(ThreadPoolCallback);
            }
        }


        internal void QueueSendNotEncrypted()
        {
            lock (_link.SendData_Lock)
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
            lock (_link.SendData_Lock)
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
            lock (_link.SendData_Lock)
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

        // Wrapper method for use with thread pool.
        internal void ThreadPoolCallback(Object threadContext)
        {

            StreamDecrypt();
        }


        /// <summary>
        /// Stops the connection
        /// </summary>
        public void Stop()
        {
            ThreadIsRunning = false;
            Dispose();
        }



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
                                            _PCollection.GivePacketToClass(new Packet_Recv(link, _rbr2));
                                        }
                                        else
                                        {
                                            Packet_Recv Recv = new Packet_Recv(link, _rbr2);
                                            Recv.Data = _rbr2.ReadBytes(size - Options.PacketHeadSize);
                                            _PCollection.GivePacketToClass(Recv);
                                        }

                                        mode = 0;

                                        action2 = true;
                                    }
                                    break;
                            }
                        }


                        byte[] lastbytes = _rbr2.ReadBytes((int)(_rms2.Length - _rms2.Position));

                        if (_rms2.Length > 1000000)
                        {
                            _rms2.Dispose();
                            _rbr2.Dispose();
                            _rbw2.Dispose();
                            _rms2 = new MemoryStream();
                            _rbr2 = new BinaryReader(_rms2);
                            _rbw2 = new BinaryWriter(_rms2);
                        }
                        else
                        {
                            _rms2.SetLength(0);
                            _rms2.Flush();
                            _rbw2.Flush();
                        }
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
            IsInQueue = false;
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

                StreamIsConnected = false;
                ThreadIsRunning = false;
            }

            if (server != null) server.RemoveFromConnectionList(this);

            _aTimer.Stop();
            _aTimer.Dispose();
            _aTimer = null;

            lock (_ReceiveDisposelock)
            {
                lock (_SendDisposelock)
                {

                    if (_SocketTCPClient != null)
                    {
                        try
                        {
                            _SocketTCPClient.Shutdown(SocketShutdown.Both);
                        }
                        catch
                        {

                        }

                    }

                    if (_AuthStream != null) _AuthStream.Close();
                    if (_sslStream != null) _sslStream.Close();
                    _SocketTCPClient.Close();


                    mySendNotEncryptedCallback = null;
                    myReceiveNotEncryptedCallback = null;
                    mySendKerberosCallback = null;
                    myReceiveKerberosCallback = null;
                    mySendSSLCallback = null;
                    myReceiveSSLCallback = null;

                }
            }




            #region WorkAtStreamDispose
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
                        //Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
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
                        //Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                    }

                    try
                    {
                        _rms1.Dispose();
                        _rms2.Dispose();

                        _rms1 = null;
                        _rms2 = null;

                        _buff = null;
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                    }
                }
            }
            #endregion



            if (link != null)
            {
                link.Dispose();
                link = null;
            }
        }





        #region Receive


        internal void ReceiveNotEncrypted(Object threadContext)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _SocketTCPClient.BeginReceive(_buff, 0, _buff.Length, 0, myReceiveNotEncryptedCallback, _SocketTCPClient);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void ReceiveNotEncryptedCallback(IAsyncResult iar)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        bytesRead = _SocketTCPClient.EndReceive(iar);
                        //doubbleDedectionRead = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            WritePackets();

            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _SocketTCPClient.BeginReceive(_buff, 0, _buff.Length, 0, myReceiveNotEncryptedCallback, _SocketTCPClient);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
        }

        internal void ReceiveSSL(Object threadContext)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveSSLCallback, _sslStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void ReceiveSSLCallback(IAsyncResult iar)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        //Socket sendingSocket = (Socket)iar.AsyncState;
                        bytesRead = _sslStream.EndRead(iar);
                        //doubbleDedectionRead = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            WritePackets();

            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _sslStream.BeginRead(_buff, 0, _buff.Length, myReceiveSSLCallback, _sslStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
        }

        internal void ReceiveKerberos(Object threadContext)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveKerberosCallback, _AuthStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void ReceiveKerberosCallback(IAsyncResult iar)
        {
            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        //Socket sendingSocket = (Socket)iar.AsyncState;
                        bytesRead = _AuthStream.EndRead(iar);
                        //doubbleDedectionRead = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            WritePackets();


            lock (_ReceiveDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedectionRead) Console.WriteLine("?????????????????????????????????????????????????????????????????????");
                    //doubbleDedectionRead = true;
                    try
                    {
                        _AuthStream.BeginRead(_buff, 0, _buff.Length, myReceiveKerberosCallback, _AuthStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
        }

        #endregion


        private void WritePackets()
        {
            if (bytesRead > 0)
            {
                lock (_ReadStream_Lock)
                {
                    if (_rbw1 != null)
                    {
                        _rbw1.Write(_buff, 0, bytesRead);

                        QueueStreamDecrypt();
                    }
                }
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
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");

            if (!StreamIsConnected)
            {
                //Dispose();
                return;
            }

            if (GetPackets()) return;

            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedection) Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    //doubbleDedection = true;
                    try
                    {
                        _SocketTCPClient.BeginSend(byteData._SendData, 0, byteData._SendData.Length, 0, mySendNotEncryptedCallback, _SocketTCPClient);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void SendNotEncryptedCallback(IAsyncResult iar)
        {
            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        _SocketTCPClient.EndSend(iar);
                        //doubbleDedection = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
            else
            {
                // Contiue sending...
                SendNotEncrypted(null);
            }
        }


        internal void SendKerberos(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");

            if (!StreamIsConnected)
            {
                return;
            }

            if (GetPackets()) return;

            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedection) Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    //doubbleDedection = true;
                    try
                    {
                        _AuthStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, mySendKerberosCallback, _AuthStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void SendKerberosCallback(IAsyncResult iar)
        {
            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        _AuthStream.EndWrite(iar);
                        //doubbleDedection = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
            else
            {
                // Contiue sending...
                SendKerberos(null);
            }
        }

        internal void SendSSL(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");

            if (!StreamIsConnected)
            {
                return;
            }

            if (GetPackets()) return;

            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    //if (doubbleDedection) Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    //doubbleDedection = true;
                    try
                    {
                        _sslStream.BeginWrite(byteData._SendData, 0, byteData._SendData.Length, mySendSSLCallback, _sslStream);
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }
            if (_DoDispose)
            {
                Dispose();
            }
        }

        private void SendSSLCallback(IAsyncResult iar)
        {
            lock (_SendDisposelock)
            {
                if (StreamIsConnected)
                {
                    try
                    {
                        _sslStream.EndWrite(iar);
                        //doubbleDedection = false;
                    }
                    catch (Exception esf)
                    {
                        StreamIsConnected = false;
                        _DoDispose = true;
                    }
                }
            }

            if (_DoDispose)
            {
                Dispose();
            }
            else
            {
                // Contiue sending...
                SendSSL(null);
            }
        }

        #endregion

        private bool GetPackets()
        {
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
                    else
                    {

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


                    return true;

                }

                return false;
            }
        }
    }

}
