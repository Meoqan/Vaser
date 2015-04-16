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

namespace Vaser
{
    public class Connection
    {

        private SemaphoreSlim _Settings_ThreadLock = new SemaphoreSlim(1);
        private bool _ThreadIsRunning = true;
        private bool _StreamIsConnected = true;
        private bool _IsServer = false;
        private NetworkStream _ConnectionStream;
        private NegotiateStream _AuthStream;
        private Thread _ProcessingDecryptThread;
        private Thread _ClientThread;
        private TcpClient _SocketTCPClient;

        private TCPServer _server;

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

        internal TCPServer server
        {
            get
            {
                _Settings_ThreadLock.Wait();
                TCPServer ret = _server;
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

        
        /// <summary>
        /// Creates a new connection for processing data
        /// </summary>
        public Connection(TcpClient client, bool IsServer, TCPServer srv = null)
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

            _SocketTCPClient = client;

            server = srv;

            // encryption
            _SocketTCPClient.LingerState = (new LingerOption(true, 0));
            // encryption END


            IPv4Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

            link = new Link();
            link.Connect = this;

            _ClientThread = new Thread(HandleClientComm);
            _ClientThread.Start();

            _ProcessingDecryptThread = new Thread(StreamDecrypt);
            _ProcessingDecryptThread.Start();

        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="Data"></param>
        internal void SendData(byte[] Data)
        {
            _WriteStream_Lock.Wait();
            if (StreamIsConnected && _sbw1 != null)
            {
                _sbw1.Write(Data);
            }
            _WriteStream_Lock.Release();
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        public void Stop()
        {
            ThreadIsRunning = false;
        }

        private void StreamDecrypt() // here is a Bug!
        {
            int mode = 0;
            int size = 0;
            bool action1 = false;
            bool action2 = false;


            while (ThreadIsRunning && Options.Operating)
            {
                _WorkAtStream_Lock.Wait();

                if (_rms1 == null)
                {
                    _WorkAtStream_Lock.Release();
                    break;
                }
                
                action1 = true;

                Portal.lock_givePacketToClass();
                while (action1)
                {
                    action1 = false;
                    _ReadStream_Lock.Wait();
                    if (_rms1.Length > 0) action1 = true;

                    _rbw2.Write(_rms1.ToArray());
                    _rms1.SetLength(0);

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

                                    // if the Packetsize is beond the limits, terminate the connection. maybe a Hacking attempt?
                                    if (size > Options.MaximumPacketSize || size < Options.PacketHeadSize)
                                    {
                                        this.Stop();
                                        mode = 100;
                                        break;
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
                    _rms2.SetLength(0);
                    _rbw2.Write(lastbytes);

                }
                Portal.release_givePacketToClass();
                _WorkAtStream_Lock.Release();


                Thread.Sleep(1);
            }

        }


        private void HandleClientComm()
        {
            

            _ConnectionStream = _SocketTCPClient.GetStream();
            // encryption
            _AuthStream = new NegotiateStream(_ConnectionStream, false);

            if (_IsServer)
            { //server
                try
                {
                    _AuthStream.AuthenticateAsServer();


                    link.IsAuthenticated = _AuthStream.IsAuthenticated;
                    link.IsEncrypted = _AuthStream.IsEncrypted;
                    link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                    link.IsSigned = _AuthStream.IsSigned;
                    link.IsServer = _AuthStream.IsServer;

                    // Display properties of the authenticated client.
                    IIdentity id = _AuthStream.RemoteIdentity;
                    Console.WriteLine("{0} was authenticated using {1}.",
                        id.Name,
                        id.AuthenticationType
                        );
                    link.UserName = id.Name;


                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }

            }
            else
            { //client
                try
                {
                    _AuthStream.AuthenticateAsClient();

                    link.IsAuthenticated = _AuthStream.IsAuthenticated;
                    link.IsEncrypted = _AuthStream.IsEncrypted;
                    link.IsMutuallyAuthenticated = _AuthStream.IsMutuallyAuthenticated;
                    link.IsSigned = _AuthStream.IsSigned;
                    link.IsServer = _AuthStream.IsServer;

                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Authentication failed. " + e.ToString());
                    Dispose();
                    return;
                }
            }
            

            //AuthenticatedStreamReporter.DisplayProperties(authStream);
            // encryption END

            int bytesRead;

            bool action = false;

            MemoryStream tmpsms = null;
            BinaryWriter tmpsbw = null;

            byte[] buff = new byte[1024 * 256];

            while (ThreadIsRunning && Options.Operating)
            {
                action = true;
                bytesRead = 0;


                try
                {
                    while (action)
                    {
                        action = false;

                        while (_ConnectionStream.DataAvailable)
                        {
                            //Console.WriteLine("Stream ReadLength b4 read" );
                            bytesRead = _AuthStream.Read(buff, 0, buff.Length);
                            //Console.WriteLine("Stream ReadLength b4 end" );
                            _ReadStream_Lock.Wait();
                            _rbw1.Write(buff, 0, bytesRead);
                            if (bytesRead > 0) action = true;
                            _ReadStream_Lock.Release();

                        }



                        //CLIENT MARKER

                        //counter = 200;
                        if (_ConnectionStream.CanWrite)
                        {
                            byte[] data = null;

                            _WriteStream_Lock.Wait();

                            tmpsms = _sms2;
                            _sms2 = _sms1;
                            _sms1 = tmpsms;

                            tmpsbw = _sbw2;
                            _sbw2 = _sbw1;
                            _sbw1 = tmpsbw;

                            _WriteStream_Lock.Release();

                            if (_sms2.Length > 0)
                            {
                                data = _sms2.ToArray();
                                _sms2.SetLength(0);

                                if (data != null)
                                {
                                    //Console.WriteLine("Data written: " + data.Length);
                                    _AuthStream.Write(data, 0, data.Length);
                                    action = true;
                                }
                            }


                        }
                        if (!_SocketTCPClient.Connected) action = false;
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.ToString());

                    //error
                    StreamIsConnected = false;
                    ThreadIsRunning = false;

                    break;
                }



                Thread.Sleep(1);


                if (!_SocketTCPClient.Connected)
                {
                    StreamIsConnected = false;
                    ThreadIsRunning = false;
                }



            }

            Dispose();

            //Console.WriteLine("Connection closed.");
        }

        internal void Dispose()
        {
            _AuthStream.Close();
            _ConnectionStream.Close();
            _SocketTCPClient.Close();

            _AuthStream.Dispose();
            _ConnectionStream.Dispose();

            StreamIsConnected = false;
            ThreadIsRunning = false;

            _WorkAtStream_Lock.Wait();
            _WriteStream_Lock.Wait();
            _ReadStream_Lock.Wait();

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

            _WriteStream_Lock.Release();
            _ReadStream_Lock.Release();
            _WorkAtStream_Lock.Release();

            if (server != null) server.RemoveFromConnectionList(this);

            if (link != null) link.Dispose();
        }

        /*public static void EndReadCallback(IAsyncResult ar)
        {
            // Get the saved data.
            ClientState cState = (ClientState)ar.AsyncState;
            TcpClient clientRequest = cState.Client;
            NegotiateStream authStream = (NegotiateStream)cState.AuthenticatedStream;
            // Get the buffer that stores the message sent by the client.
            int bytes = -1;
            // Read the client message.
            try
            {
                bytes = authStream.EndRead(ar);
                cState.Message.Append(Encoding.UTF8.GetChars(cState.Buffer, 0, bytes));
                if (bytes != 0)
                {
                    authStream.BeginRead(cState.Buffer, 0, cState.Buffer.Length,
                          new AsyncCallback(EndReadCallback),
                          cState);
                    return;
                }
            }
            catch (Exception e)
            {
                // A real application should do something
                // useful here, such as logging the failure.
                Console.WriteLine("Client message exception:");
                Console.WriteLine(e);
                cState.Waiter.Set();
                return;
            }
            IIdentity id = authStream.RemoteIdentity;
            Console.WriteLine("{0} says {1}", id.Name, cState.Message.ToString());
            cState.Waiter.Set();
        }*/
    }

    public class AuthenticatedStreamReporter
    {
        public static void DisplayProperties(AuthenticatedStream stream)
        {
            Console.WriteLine("IsAuthenticated: {0}", stream.IsAuthenticated);
            Console.WriteLine("IsMutuallyAuthenticated: {0}", stream.IsMutuallyAuthenticated);
            Console.WriteLine("IsEncrypted: {0}", stream.IsEncrypted);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("IsServer: {0}", stream.IsServer);
        }
    }
}
