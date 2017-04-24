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
        public volatile bool ThreadIsRunning = true;
        public volatile bool StreamIsConnected = true;
        public volatile bool IsServer = false;
        private NetworkStream _ConnectionStream;
        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        //private NetworkStream _NotEncryptedStream;

        private Socket _SocketTCPClient;
        public volatile bool Disposed;
        public volatile bool BootupDone = false;

        internal delegate void QueueSendHidden();
        internal QueueSendHidden QueueSend = null;

        internal PortalCollection _PCollection = null;

        private int bytesRead;

        private byte[] _buff = new byte[65012];

        //private VaserServer _server;

        //private Link _link;
        //private IPAddress _IPv4Address;

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

        private object _IsInQueue_Lock = new object();
        private bool IsInQueue = false;
        private bool IsInSendQueue = false;
        
        private Timer _BootUpTimer = null;
        private int _BootUpTimes = 0;

        private byte[] _timeoutdata = BitConverter.GetBytes((int)(-1));
        private Packet_Send _timeoutpacket = new Packet_Send(BitConverter.GetBytes((int)(-1)), false);

        Packet_Send byteData = null;

        bool SendFound = false;
        bool _CallEmptyBuffer = false;
        private object _SendDisposelock = new object();
        private object _ReceiveDisposelock = new object();

        private int mode = 0;
        private int size = 0;
        private bool action1 = false;
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

            lock (_ReadStream_Lock)
            {
                _rms1 = new MemoryStream();
                _rbw1 = new BinaryWriter(_rms1);
                _rbr1 = new BinaryReader(_rms1);

                _rms2 = new MemoryStream();
                _rbw2 = new BinaryWriter(_rms2);
                _rbr2 = new BinaryReader(_rms2);

            }

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

                lock (_ReceiveDisposelock)
                {
                    lock (_SendDisposelock)
                    {
                        try
                        {
                            _SocketTCPClient.Dispose();

                            if(_ConnectionStream != null) _ConnectionStream.Dispose();

                            // encryption
                            if (_Mode == VaserOptions.ModeKerberos && _AuthStream != null)
                            {
                                _AuthStream.Dispose();
                            }

                            if (_Mode == VaserOptions.ModeSSL && _sslStream != null)
                            {
                                _sslStream.Dispose();
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
                
            }
            catch (AuthenticationException e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Dispose();

                Dispose();
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Authentication failed. " + e.ToString());
                _BootUpTimer.Dispose();

                Dispose();
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

        

        internal void QueueStreamDecrypt()
        {
            lock (_IsInQueue_Lock)
            {
                if (IsInQueue == false)
                {
                    IsInQueue = true;
                    ThreadPool.QueueUserWorkItem(ThreadPoolCallback);
                }
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
                            lock (_IsInQueue_Lock)
                            {
                                if (_rms1.Length > 0)
                                {
                                    action1 = true;
                                }
                                else
                                {
                                    IsInQueue = false;
                                }
                            }
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
            
            lock (_ReceiveDisposelock)
            {
                lock (_SendDisposelock)
                {
                    if (_SocketTCPClient != null && _SocketTCPClient.Connected)
                    {
                        try
                        {
                            _SocketTCPClient.Shutdown(SocketShutdown.Both);
                        }
                        catch
                        {

                        }

                    }

                    if (_AuthStream != null) _AuthStream.Dispose();
                    if (_sslStream != null) _sslStream.Dispose();
                    if (_ConnectionStream != null) _ConnectionStream.Dispose();
                }
            }

            

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

                        _buff = null;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Connection.Dispose()  > " + e.ToString());
                    }


                }
            }

            

            if (link != null)
            {
                link.Dispose();
                //link = null;
            }

            Debug.WriteLine("Link.Dispose finished");

        }


        private ArraySegment<byte> _SAbuff = new ArraySegment<byte>(new byte[65007]);
        internal async void ReceiveNotEncrypted(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await _SocketTCPClient.ReceiveAsync(_SAbuff, SocketFlags.None);
                    if (bytesRead < 1)
                    {
                        Dispose();
                        return;
                    }
                    _buff = _SAbuff.Array;
                    WritePackets();
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

        internal async void ReceiveSSL(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await _sslStream.ReadAsync(_buff, 0, _buff.Length);
                    if (bytesRead < 1)
                    {
                        Dispose();
                        return;
                    }
                    WritePackets();
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

        internal async void ReceiveKerberos(object state)
        {
            //Console.WriteLine("Receive");
            try
            {
                while (true)
                {
                    
                    bytesRead = await _AuthStream.ReadAsync(_buff, 0, _buff.Length);
                    if (bytesRead < 1)
                    {
                        Dispose();
                        return;
                    }
                    WritePackets();
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

        // *********************************************************
        // WARNING: if you get an AccessValidation error, check following:
        // - do you try to send data to a connecting or closed stream?
        // - do you try to send data with multiple threads at the same time?
        // - do you try to send and receive data with the same thread?
        // - RTFM! No no no, listen READ THE F MANUAL: https://msdn.microsoft.com/de-de/library/fx6588te%28v=vs.110%29.aspx
        // *********************************************************
        internal async void SendNotEncrypted(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
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
                Dispose();

                Debug.WriteLine("Connection.Send()  >" + e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        internal async void SendKerberos(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets()) return;

                    if (!StreamIsConnected) return;

                    if (_AuthStream != null && _AuthStream.CanWrite && _SocketTCPClient.Connected)
                    {
                        await _AuthStream.WriteAsync(byteData._SendData, 0, byteData._SendData.Length);
                    }

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

        internal async void SendSSL(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets()) return;

                    if (!StreamIsConnected) return;

                    if (_sslStream != null && _sslStream.CanWrite && _SocketTCPClient.Connected)
                    {
                        await _sslStream.WriteAsync(byteData._SendData, 0, byteData._SendData.Length);
                    }
                    

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


        private bool GetPackets()
        {
            SendFound = false;
            lock (link.SendData_Lock)
            {
                for (int x = 0; x < link.SendDataPortalArrayOUTPUT.Length; x++)
                {
                    if (link.SendDataPortalArrayOUTPUT[x].Count > 0)
                    {

                        //Debug.WriteLine("data");
                        byteData = link.SendDataPortalArrayOUTPUT[x].Dequeue();
                        SendFound = true;
                        if (byteData._CallEmpybuffer) _CallEmptyBuffer = true;

                        //Debug.WriteLine("Sending.... Lenght: " + byteData._SendData.Length);
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
