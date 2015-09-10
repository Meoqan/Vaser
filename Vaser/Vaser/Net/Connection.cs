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
using Vaser.global;
using System.Security.Authentication;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Timers;

namespace Vaser
{
    public class Connection
    {

        private SemaphoreSlim _Settings_ThreadLock = new SemaphoreSlim(1);
        private object _DisposeLock = new object();
        private bool _ThreadIsRunning = true;
        private bool _StreamIsConnected = true;
        private bool _IsServer = false;
        private NetworkStream _ConnectionStream;
        private NegotiateStream _AuthStream;
        private SslStream _sslStream;
        //private Thread _ProcessingDecryptThread;
        //private Thread _ClientThread;
        private TcpClient _SocketTCPClient;
        private bool _Disposed;
        private bool _BootupDone = false;

        private int bytesRead;
        private MemoryStream tmpsms = null;
        private BinaryWriter tmpsbw = null;

        private byte[] _buff = new byte[65012];

        private VaserServer _server;

        private Link _link;
        private IPAddress _IPv4Address;

        private SemaphoreSlim _WorkAtStream_Lock = new SemaphoreSlim(1);

        private SemaphoreSlim _WriteStream_Lock = new SemaphoreSlim(1);
        private MemoryStream _sms1 = null;
        private BinaryWriter _sbw1 = null;
        private MemoryStream _sms2 = null;
        private BinaryWriter _sbw2 = null;

        private SemaphoreSlim _ReadStream_Lock = new SemaphoreSlim(1);
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

        /// <summary>
        /// the IPAdress of the remote end point
        /// </summary>
        public IPAddress IPv4Address
        {
            get
            {
                _Settings_ThreadLock.Wait();
                IPAddress ret = _IPv4Address;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _IPv4Address = value;
                _Settings_ThreadLock.Release();
            }
        }

        /// <summary>
        /// Link of the connection
        /// </summary>
        public Link link
        {
            get
            {
                _Settings_ThreadLock.Wait();
                Link ret = _link;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _link = value;
                _Settings_ThreadLock.Release();
            }
        }

        internal VaserServer server
        {
            get
            {
                _Settings_ThreadLock.Wait();
                VaserServer ret = _server;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _server = value;
                _Settings_ThreadLock.Release();
            }
        }

        /// <summary>
        /// Gets or sets is the thread running
        /// </summary>
        public bool ThreadIsRunning
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _ThreadIsRunning;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _ThreadIsRunning = value;
                _Settings_ThreadLock.Release();
            }
        }

        /// <summary>
        /// is the connectionstream to the remotendpoint online.
        /// you must send data to ensure that the stream is not dead.
        /// </summary>
        public bool StreamIsConnected
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _StreamIsConnected;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _StreamIsConnected = value;
                _Settings_ThreadLock.Release();
            }
        }

        public bool Disposed
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _Disposed;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _Disposed = value;
                _Settings_ThreadLock.Release();
            }
        }


        public bool BootupDone
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _BootupDone;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _BootupDone = value;
                _Settings_ThreadLock.Release();
            }
        }
        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(TcpClient client, bool IsServer, VaserOptions Mode, X509Certificate2 Cert = null, X509Certificate2Collection cCollection = null, string targetHostname = null, VaserServer srv = null)
        {
            _IsServer = IsServer;

            _WriteStream_Lock.Wait();
            _ReadStream_Lock.Wait();

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

            _WriteStream_Lock.Release();
            _ReadStream_Lock.Release();

            _aTimer = new System.Timers.Timer(1000);
            _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _aTimer.Enabled = true;

            _Mode = Mode;

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
                _Settings_ThreadLock.Wait();
                bool ret = _IsInQueue;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _IsInQueue = value;
                _Settings_ThreadLock.Release();
            }
        }

        internal bool IsInSendQueue
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _IsInSendQueue;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _IsInSendQueue = value;
                _Settings_ThreadLock.Release();
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
            if (IsInSendQueue == false)
            {
                IsInSendQueue = true;
                ThreadPool.QueueUserWorkItem(Send);
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
            
            if (StreamIsConnected && _sbw1 != null)
            {
                _WriteStream_Lock.Wait();
                _sbw1.Write(Data);
                QueueSend();
                _WriteStream_Lock.Release();
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
                _WorkAtStream_Lock.Wait();

                if (_rms1 == null)
                {
                    _WorkAtStream_Lock.Release();
                    return;
                }

                action1 = true;

                Portal.lock_givePacketToClass();
                while (action1)
                {
                    action1 = false;
                    _ReadStream_Lock.Wait();
                    if (_rms1.Length > 0) action1 = true;

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
                        GC.Collect();
                    }

                    IsInQueue = false;
                    _ReadStream_Lock.Release();
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
                                        Portal.givePacketToClass(new Packet_Recv(link, _rbr2), null);
                                    }
                                    else
                                    {
                                        Portal.givePacketToClass(new Packet_Recv(link, _rbr2), _rbr2.ReadBytes(size - Options.PacketHeadSize));
                                    }

                                    mode = 0;

                                    action2 = true;
                                }
                                break;
                        }
                    }


                    byte[] lastbytes = _rbr2.ReadBytes((int)(_rms2.Length - _rms2.Position));

                    //_rms2.SetLength(0);
                    //_rms2.Flush();
                    //_rbw2.Flush();

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
                        GC.Collect();
                    }

                    _rbw2.Write(lastbytes);

                }
                Portal.release_givePacketToClass();
                _WorkAtStream_Lock.Release();

            }
            catch (Exception e)
            {
                try
                {
                    _WorkAtStream_Lock.Release();
                }
                catch (Exception exl)
                { }
                try
                {
                    _ReadStream_Lock.Release();
                }
                catch (Exception exl)
                { }
                try
                {
                    Portal.release_givePacketToClass();
                }
                catch (Exception exl)
                { }
                Debug.WriteLine(e.ToString());
                //Dispose();
                ThreadIsRunning = false;
            }

        }

    
        /// <summary>
        /// Handles the connection process of clients
        /// </summary>
        private void HandleClientComm()
        {
            
            _ConnectionStream = _SocketTCPClient.GetStream();
            BootupDone = true;

            // encryption
            if (_Mode == VaserOptions.ModeKerberos)
            {
                _AuthStream = new NegotiateStream(_ConnectionStream, false);
            }

            if (_Mode == VaserOptions.ModeSSL)
            {
                _sslStream = new SslStream(_ConnectionStream, false);
            }

            if (_IsServer)
            { //server
                try
                {

                    if(_Mode == VaserOptions.ModeKerberos)
                    {
                        _AuthStream.AuthenticateAsServer();

                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        // Display properties of the authenticated client.
                        IIdentity id = _AuthStream.RemoteIdentity;
                        Debug.WriteLine("{0} was authenticated using {1}.",
                            id.Name,
                            id.AuthenticationType
                            );
                        link.UserName = id.Name;
                    }


                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        _sslStream.AuthenticateAsServer(_Cert,false,SslProtocols.Tls12,false);
                        link.IsEncrypted = true;
                        link.IsServer = true;
                    }

                    server.AddNewLink(link);
                }
                catch (AuthenticationException e)
                {
                    Debug.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }

            }
            else
            { //client
                try
                {
                    if (_Mode == VaserOptions.ModeKerberos)
                    {
                        _AuthStream.AuthenticateAsClient();

                        link.IsAuthenticated = _AuthStream.IsAuthenticated;
                        link.IsEncrypted = _AuthStream.IsEncrypted;
                        link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                        link.IsSigned = _AuthStream.IsSigned;
                        link.IsServer = _AuthStream.IsServer;

                        IIdentity id = _AuthStream.RemoteIdentity;
                        Debug.WriteLine("{0} was authenticated using {1}.",
                            id.Name,
                            id.AuthenticationType
                            );
                    }

                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        _sslStream.AuthenticateAsClient(_targetHostname,_CertCol, SslProtocols.Tls12,false);
                        link.IsEncrypted = true;
                    }
                    

                }
                catch (AuthenticationException e)
                {
                    Debug.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
            }

            
            // encryption END

            Receive();
            //Send();

        }

        private static void DisconnectCallback(IAsyncResult ar)
        {
            // Complete the disconnect request.
            Socket client = (Socket)ar.AsyncState;
            client.EndDisconnect(ar);
            Debug.WriteLine("Disconnected.");

        }

        internal void Dispose()
        {
            lock(_DisposeLock)
            {
                Debug.WriteLine("Link.Dispose called");
                if (Disposed)
                {
                    Debug.WriteLine("Link.Dispose abort");
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
                        Debug.WriteLine(exs.ToString());
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

                _WorkAtStream_Lock.Wait();
                _WriteStream_Lock.Wait();
                _ReadStream_Lock.Wait();

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
                _WriteStream_Lock.Release();
                _ReadStream_Lock.Release();
                _WorkAtStream_Lock.Release();

                if (server != null) server.RemoveFromConnectionList(this);

                if (link != null) link.Dispose();

                GC.Collect();

                Debug.WriteLine("Link.Dispose finished");
            }
        }
        

        private void Receive()
        {
            try
            {
                //Console.WriteLine("Stream ReadLength b4 read" );
                
                // Begin receiving the data from the remote device.
                if (_AuthStream != null) _AuthStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _AuthStream);
                if (_sslStream != null) _sslStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _sslStream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Dispose();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Read data from the remote device.
                if (_AuthStream != null) bytesRead = _AuthStream.EndRead(ar);
                if (_sslStream != null) bytesRead = _sslStream.EndRead(ar);

                //Console.WriteLine("reading");
                if (bytesRead > 0)
                {
                    //Console.WriteLine("{0} bytes read by the Connection.", bytesRead);
                    _ReadStream_Lock.Wait();
                    _rbw1.Write(_buff, 0, bytesRead);
                    _ReadStream_Lock.Release();


                    QueueStreamDecrypt();

                    // Get the rest of the data.
                    if (_AuthStream != null) _AuthStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _AuthStream);
                    if (_sslStream != null) _sslStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _sslStream);
                }
                else
                {
                    if (_AuthStream != null) _AuthStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _AuthStream);
                    if (_sslStream != null) _sslStream.BeginRead(_buff, 0, _buff.Length, new AsyncCallback(ReceiveCallback), _sslStream);
                }
            }
            catch (Exception e)
            {
                try
                {
                    _ReadStream_Lock.Release();
                }
                catch (Exception exl)
                { }
                Debug.WriteLine(e.ToString());
                Dispose();
            }
        }

        byte[] byteData = null;
        internal void Send(Object threadContext)
        {
            try
            {
                //do
                //{
                    //if (_sms2.Position == 0) Thread.Sleep(1);
                if (_sms1.Position == 0)
                {
                    IsInSendQueue = false;
                    return;
                }

                _WriteStream_Lock.Wait();

                tmpsms = _sms2;
                _sms2 = _sms1;
                _sms1 = tmpsms;

                tmpsbw = _sbw2;
                _sbw2 = _sbw1;
                _sbw1 = tmpsbw;

                _WriteStream_Lock.Release();

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
                    GC.Collect();
                }
                


                //} while (byteData.Length == 0);

                if (_AuthStream != null) _AuthStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _AuthStream);
                if (_sslStream != null) _sslStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _sslStream);
            }
            catch (Exception e)
            {
                try
                {
                    _WriteStream_Lock.Release();
                }
                catch (Exception exl)
                { }
                Debug.WriteLine(e.ToString());
                Dispose();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {

                // Complete sending the data to the remote device.
                if (_AuthStream != null) _AuthStream.EndWrite(ar);
                if (_sslStream != null) _sslStream.EndWrite(ar);
                //Console.WriteLine("Sent bytes to server.");

                //do
                //{
                if (_sms1.Position == 0)
                {
                    IsInSendQueue = false;
                    return;
                }

                _WriteStream_Lock.Wait();

                tmpsms = _sms2;
                _sms2 = _sms1;
                _sms1 = tmpsms;

                tmpsbw = _sbw2;
                _sbw2 = _sbw1;
                _sbw1 = tmpsbw;

                _WriteStream_Lock.Release();

                //if (_sms2.Length > 0)
                //{
                byteData = _sms2.ToArray();
                _sms2.SetLength(0);
                _sms2.Flush();
                _sbw2.Flush();
                //} while (byteData.Length == 0);

                if (_AuthStream != null) _AuthStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _AuthStream);
                if (_sslStream != null) _sslStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), _sslStream);
            }
            catch (Exception e)
            {
                try
                {
                    _WriteStream_Lock.Release();
                }
                catch (Exception exl)
                { }
                Debug.WriteLine(e.ToString());
                Dispose();
            }
        }
    }
    
}
