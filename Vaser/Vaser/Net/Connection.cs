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
        private MemoryStream tmpsms = null;
        private BinaryWriter tmpsbw = null;

        private byte[] _buff = new byte[65012];

        private VaserServer _server;

        private Link _link;
        private IPAddress _IPv4Address;

        private object _WorkAtStream_Lock = new object();

        //private SemaphoreSlim _WriteStream_Lock = new SemaphoreSlim(1);
        private object _WriteStream_Lock = new object();
        private MemoryStream _sms1 = null;
        private BinaryWriter _sbw1 = null;
        private MemoryStream _sms2 = null;
        private BinaryWriter _sbw2 = null;

        //private SemaphoreSlim _ReadStream_Lock = new SemaphoreSlim(1);
        private object _ReadStream_Lock = new object();
        private MemoryStream _rms1 = null;
        private BinaryWriter _rbw1 = null;
        private BinaryReader _rbr1 = null;

        private MemoryStream _rms2 = null;
        private BinaryWriter _rbw2 = null;
        private BinaryReader _rbr2 = null;

        private VaserOptions _Mode = null;
        private X509Certificate2 _Cert = null;
        private X509Certificate2Collection _CertCol = null;
        private string _targetHostname = string.Empty;

        private bool _IsInQueue = false;
        private bool _IsInSendQueue = false;

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
        /*
        /// <summary>
        /// Gets or sets is the thread running
        /// </summary>
        public bool ThreadIsRunning
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _ThreadIsRunning;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _ThreadIsRunning = value;
                }
            }
        }*/
        /*
        /// <summary>
        /// is the connectionstream to the remotendpoint online.
        /// you must send data to ensure that the stream is not dead.
        /// </summary>
        public bool StreamIsConnected
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _StreamIsConnected;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _StreamIsConnected = value;
                }
            }
        }*/

        /*public bool Disposed
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _Disposed;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _Disposed = value;
                }
            }
        }


        public bool BootupDone
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _BootupDone;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _BootupDone = value;
                }
            }
        }*/
        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(TcpClient client, bool _IsServer, VaserOptions Mode, PortalCollection PColl, X509Certificate2 Cert = null, X509Certificate2Collection cCollection = null, string targetHostname = null, VaserServer srv = null)
        {
            IsServer = _IsServer;

            //_WriteStream_Lock.Wait();
            //_ReadStream_Lock.Wait();
            lock (_WriteStream_Lock)
            {
                lock (_ReadStream_Lock)
                {
                    _rms1 = new MemoryStream();
                    _rbw1 = new BinaryWriter(_rms1);
                    _rbr1 = new BinaryReader(_rms1);

                    _rms2 = new MemoryStream();
                    _rbw2 = new BinaryWriter(_rms2);
                    _rbr2 = new BinaryReader(_rms2);

                    _sms1 = new MemoryStream();
                    _sbw1 = new BinaryWriter(_sms1);
                    _sms2 = new MemoryStream();
                    _sbw2 = new BinaryWriter(_sms2);
                }
            }
            //_WriteStream_Lock.Release();
            //_ReadStream_Lock.Release();

            _aTimer = new System.Timers.Timer(500);
            _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            _Mode = Mode;
            _PCollection = PColl;

            _Cert = Cert;
            _CertCol = cCollection;
            _targetHostname = targetHostname;

            _SocketTCPClient = client;

            server = srv;

            // encryption
            _SocketTCPClient.LingerState = (new LingerOption(true, 0));
            // encryption END


            IPv4Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            link = new Link();
            link.Connect = this;

            HandleClientComm();
        }
        /// <summary>
        /// Handles the connection process of clients
        /// </summary>
        private void HandleClientComm()
        {

            try
            {

                _ConnectionStream = _SocketTCPClient.GetStream();


                // encryption
                if (_Mode == VaserOptions.ModeKerberos)
                {
                    _AuthStream = new NegotiateStream(_ConnectionStream, false);
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    _sslStream = new SslStream(_ConnectionStream, false);
                }

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    _NotEncryptedStream = _ConnectionStream;
                }

                if (IsServer)
                { //server


                    if (_Mode == VaserOptions.ModeKerberos)
                    {
                        _AuthStream.AuthenticateAsServer();

                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        // Display properties of the authenticated client.
                        IIdentity id = _AuthStream.RemoteIdentity;
                        /*Debug.WriteLine("{0} was authenticated using {1}.",
                            id.Name,
                            id.AuthenticationType
                            );*/
                        link.UserName = id.Name;
                    }


                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        _sslStream.AuthenticateAsServer(_Cert, false, SslProtocols.Tls12, false);
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
                        _AuthStream.AuthenticateAsClient();

                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        IIdentity id = _AuthStream.RemoteIdentity;
                        /*Debug.WriteLine("{0} was authenticated using {1}.",
                            id.Name,
                            id.AuthenticationType
                            );*/
                    }

                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        _sslStream.AuthenticateAsClient(_targetHostname, _CertCol, SslProtocols.Tls12, false);
                        link.IsEncrypted = true;
                    }

                    //Thread.Sleep(50);
                    BootupDone = true;
                }

                _IsAccepted = true;
                Receive();

                _aTimer.Enabled = true;

            }
            catch (AuthenticationException e)
            {
                //Debug.WriteLine("Authentication failed. " + e.ToString());
                Dispose();
                return;
            }
            catch (Exception e)
            {
                //Debug.WriteLine("Authentication failed. " + e.ToString());
                Dispose();
                return;
            }
            // encryption END



            //Send();

            //Receive();
            //ThreadPool.QueueUserWorkItem(SendAsync);
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

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //Debug.WriteLine("Send keep alive packet {0}", e.SignalTime);
            SendData(_timeoutdata);
        }

        internal bool IsInQueue
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _IsInQueue;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _IsInQueue = value;
                }
            }
        }

        internal bool IsInSendQueue
        {
            get
            {
                lock (_Settings_ThreadLock)
                {
                    return _IsInSendQueue;
                }
            }
            set
            {
                lock (_Settings_ThreadLock)
                {
                    _IsInSendQueue = value;
                }
            }
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
            lock (_Settings_ThreadLock)
            {
                if (_IsInSendQueue == false)
                {
                    _IsInSendQueue = true;
                    ThreadPool.QueueUserWorkItem(Send);
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
        internal void SendData(byte[] Data)
        {
            lock (_WriteStream_Lock)
            {
                if (StreamIsConnected && _sbw1 != null)
                {
                    //_WriteStream_Lock.Wait();

                    _sbw1.Write(Data);
                    QueueSend();
                }
                //_WriteStream_Lock.Release();
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

                            if (_rms1.Length < 10000000)
                            {
                                _rms1.SetLength(0);
                                _rms1.Flush();
                                _rbw1.Flush();
                            }
                            else
                            {
                                _rms1.Dispose();
                                _rbw1.Dispose();
                                _rbr1.Dispose();
                                _rms1 = new MemoryStream();
                                _rbw1 = new BinaryWriter(_rms1);
                                _rbr1 = new BinaryReader(_rms1);
                                //GC.Collect();
                            }

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

                        if (_rms2.Length < 10000000)
                        {
                            _rms2.SetLength(0);
                            _rms2.Flush();
                            _rbw2.Flush();
                        }
                        else
                        {
                            _rms2.Dispose();
                            _rbw2.Dispose();
                            _rbr2.Dispose();
                            _rms2 = new MemoryStream();
                            _rbw2 = new BinaryWriter(_rms1);
                            _rbr2 = new BinaryReader(_rms1);
                        }

                        _rbw2.Write(lastbytes);

                    }

                }

            }
            catch (Exception e)
            {


                //Debug.WriteLine(e.ToString());
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
                //Debug.WriteLine("Disconnected.");
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e.ToString());
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
                if (_SocketTCPClient != null && _SocketTCPClient.Connected)
                {
                    try
                    {
                        _SocketTCPClient.Client.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception exs)
                    {
                        //Debug.WriteLine(exs.ToString());
                    }

                }
                if (_AuthStream != null) _AuthStream.Close();
                if (_sslStream != null) _sslStream.Close();
                _ConnectionStream.Close();
                _SocketTCPClient.Close();

                if (_AuthStream != null) _AuthStream.Dispose();
                if (_sslStream != null) _sslStream.Dispose();
                _ConnectionStream.Dispose();

                StreamIsConnected = false;
                ThreadIsRunning = false;

                lock (_WorkAtStream_Lock)
                {
                    //_WriteStream_Lock.Wait();
                    lock (_WriteStream_Lock)
                    {
                        //_ReadStream_Lock.Wait();
                        lock (_ReadStream_Lock)
                        {
                            try
                            {
                                //_buff = null;

                                _rbr1.Dispose();
                                _rbr2.Dispose();

                                _sbw1.Dispose();
                                _sbw2.Dispose();
                                _rbw1.Dispose();
                                _rbw2.Dispose();

                                _sms1.Dispose();
                                _sms2.Dispose();
                                _rms1.Dispose();
                                _rms2.Dispose();

                                _sms1 = null;
                                _sms2 = null;
                                _rms1 = null;
                                _rms2 = null;

                                _aTimer.Stop();
                                _aTimer.Dispose();
                            }
                            catch (Exception e)
                            {
                                //Console.WriteLine(e.ToString());

                            }
                        }
                    }
                }

                if (server != null) server.RemoveFromConnectionList(this);

                if (link != null) link.Dispose();

                //Debug.WriteLine("Link.Dispose finished");
            }
        }


        byte[] byteData = null;
        private void Receive()
        {
            try
            {
                //Console.WriteLine("Stream ReadLength b4 read" );

                // Begin receiving the data from the remote device.
                if (_AuthStream != null)
                {
                    _AuthStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _AuthStream);
                }
                if (_sslStream != null)
                {
                    _sslStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _sslStream);
                }
                if (_NotEncryptedStream != null)
                {
                    _NotEncryptedStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _NotEncryptedStream);
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Dispose();

                //Debug.WriteLine(e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!BootupDone) throw new Exception("Data was recived b4 connection was booted.");
            try
            {

                // Read data from the remote device.
                if (_AuthStream != null)
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
                }


                if (bytesRead > 0)
                {
                    //Debug.WriteLine("{0} bytes read by the Connection.", bytesRead);

                    lock (_ReadStream_Lock)
                    {
                        _rbw1.Write(_buff, 0, bytesRead);

                        QueueStreamDecrypt();
                    }

                }

                // Get the rest of the data.
                if (_AuthStream != null && _AuthStream.CanRead)
                {
                    _AuthStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _AuthStream);
                }
                if (_sslStream != null && _sslStream.CanRead)
                {
                    _sslStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _sslStream);
                }

                if (_NotEncryptedStream != null && _NotEncryptedStream.CanRead)
                {
                    _NotEncryptedStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _NotEncryptedStream);
                }

            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                //_AuthStream = null;
                //_sslStream = null;

                Dispose();

                //Console.WriteLine(e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

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
                //if (_AuthStream == null && _sslStream == null && _NotEncryptedStream == null) return;

                if (_sms1.Position == 0)
                {
                    IsInSendQueue = false;
                    return;
                }

                lock (_WriteStream_Lock)
                {
                    tmpsms = _sms2;
                    _sms2 = _sms1;
                    _sms1 = tmpsms;

                    tmpsbw = _sbw2;
                    _sbw2 = _sbw1;
                    _sbw1 = tmpsbw;
                }

                byteData = _sms2.ToArray();

                if (_sms2.Length < 10000000)
                {
                    _sms2.SetLength(0);
                    _sms2.Flush();
                    _sbw2.Flush();
                }
                else
                {
                    _sms2.Dispose();
                    _sbw2.Dispose();
                    _sms2 = new MemoryStream();
                    _sbw2 = new BinaryWriter(_sms2);
                }


                if (!StreamIsConnected) return;

                if (_AuthStream != null && _AuthStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _AuthStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _AuthStream);
                }
                if (_sslStream != null && _sslStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _sslStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _sslStream);
                }

                if (_NotEncryptedStream != null && _NotEncryptedStream.CanWrite && _SocketTCPClient.Connected)
                {
                    _NotEncryptedStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _NotEncryptedStream);
                }
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Dispose();

                //Debug.WriteLine(e.ToString());
                //if (e.InnerException != null) Console.WriteLine("Inner exception: {0}", e.InnerException);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {

            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {
                // Complete sending the data to the remote device.
                if (_AuthStream != null) _AuthStream.EndWrite(ar);
                if (_sslStream != null) _sslStream.EndWrite(ar);
                if (_NotEncryptedStream != null) _NotEncryptedStream.EndWrite(ar);

                // Contiue sending...
                Send(null);
            }
            catch (Exception e)
            {
                StreamIsConnected = false;
                Dispose();
            }
        }
    }

}
