﻿using System;
using System.Collections.Generic;
using Windows.System.Threading;
using System.IO;
using System.Net;
using System.Net.Security;
using Windows.Networking.Sockets;
using Windows.Networking;
using System.Security.Authentication;
using System.Security.Principal;
using System.Diagnostics;
using Windows.Storage.Streams;

using Vaser.ConnectionSettings;

namespace Vaser
{
    internal class Connection
    {
        private object _DisposeLock = new object();
        public volatile bool ThreadIsRunning = true;
        public volatile bool StreamIsConnected = true;
        public volatile bool IsServer = false;

        private StreamSocket _SocketTCPClient;
        private Stream inStream;
        private DataWriter outStream;

        public volatile bool Disposed;
        public volatile bool BootupDone = false;

        internal delegate void QueueSendHidden();
        internal QueueSendHidden QueueSend = null;

        internal PortalCollection _PCollection = null;

        private int bytesRead;

        private byte[] _buff = new byte[65012];

        private VaserServer _server;

        private Link _link;
        private HostName _IPv4Address;

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

        private System.Threading.Timer _aTimer;
        private System.Threading.Timer _BootUpTimer = null;
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
        public HostName IPv4Address
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
        public Connection(StreamSocket client, bool _IsServer, VaserOptions Mode, PortalCollection PColl, VaserKerberosServer KerberosS, VaserSSLServer SSLS, VaserKerberosClient KerberosC, VaserSSLClient SSLC, VaserServer srv = null)
        {
            //Debug.WriteLine("Init Connection class");
            IsServer = _IsServer;


            //Debug.WriteLine("Init Streams");
            lock (_ReadStream_Lock)
            {
                _rms1 = new MemoryStream();
                _rbw1 = new BinaryWriter(_rms1);
                _rbr1 = new BinaryReader(_rms1);

                _rms2 = new MemoryStream();
                _rbw2 = new BinaryWriter(_rms2);
                _rbr2 = new BinaryReader(_rms2);

            }
            //Debug.WriteLine("Init Streams Done");

            //Debug.WriteLine("Init Options");
            _Mode = Mode;
            _PCollection = PColl;

            _vSSLS = SSLS;
            _vKerberosS = KerberosS;
            _vSSLC = SSLC;
            _vKerberosC = KerberosC;

            _SocketTCPClient = client;

            server = srv;
            //Debug.WriteLine("Init Options Done");
            IPv4Address = client.Information.RemoteAddress;

            //Debug.WriteLine("Create Link");
            link = new Link(PColl);
            link.Connect = this;
            //Debug.WriteLine("Create Link Done");

            //Debug.WriteLine("Init Connection class DONE");
            if (_IsServer)
            {
                //Debug.WriteLine("Send to HandleClientComm");
                ThreadPool.RunAsync(HandleClientComm);
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
            _BootUpTimer = new System.Threading.Timer(new System.Threading.TimerCallback(_BootUpTimer_Elapsed), null, 100, 100);

            bool leaveInnerStreamOpen = false;

            try
            {

                // encryption
                if (_Mode == VaserOptions.ModeKerberos)
                {
                    QueueSend = QueueSendKerberos;
                }

                if (_Mode == VaserOptions.ModeSSL)
                {
                    QueueSend = QueueSendSSL;
                }

                if (_Mode == VaserOptions.ModeNotEncrypted)
                {
                    QueueSend = QueueSendNotEncrypted;
                }
                
                if (IsServer)
                { //server


                    if (_Mode == VaserOptions.ModeKerberos)
                    {


                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);

                        link.IsAuthenticated = false;
                        link.IsEncrypted = false;
                        link.IsMutuallyAuthenticated = false;
                        link.IsSigned = false;
                        link.IsServer = false;

                        link.UserName = "none";
                    }


                    if (_Mode == VaserOptions.ModeSSL)
                    {
                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);

                        link.IsEncrypted = true;
                        link.IsServer = true;
                    }

                    if (_Mode == VaserOptions.ModeNotEncrypted)
                    {
                        
                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);

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
                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);

                        link.IsAuthenticated = false;
                        link.IsEncrypted = false;
                        link.IsMutuallyAuthenticated = false;
                        link.IsSigned = false;
                        link.IsServer = false;

                    }

                    if (_Mode == VaserOptions.ModeSSL)
                    {

                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);


                        link.IsEncrypted = true;
                    }


                    if (_Mode == VaserOptions.ModeNotEncrypted)
                    {

                        //Read line from the remote client.
                        inStream = _SocketTCPClient.InputStream.AsStreamForRead();

                        //Send the line back to the remote client.
                        outStream = new DataWriter(_SocketTCPClient.OutputStream);

                    }

                    //Thread.Sleep(50);
                    BootupDone = true;

                    _IsAccepted = true;
                    if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.RunAsync(ReceiveNotEncrypted);
                    if (_Mode == VaserOptions.ModeKerberos) ThreadPool.RunAsync(ReceiveKerberos);
                    if (_Mode == VaserOptions.ModeSSL) ThreadPool.RunAsync(ReceiveSSL);
                }

                _aTimer = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimedEvent), null, 5000, 5000);

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
                //new Thread(Receive).Start();
                if (_Mode == VaserOptions.ModeNotEncrypted) ThreadPool.RunAsync(ReceiveNotEncrypted);
                if (_Mode == VaserOptions.ModeKerberos) ThreadPool.RunAsync(ReceiveKerberos);
                if (_Mode == VaserOptions.ModeSSL) ThreadPool.RunAsync(ReceiveSSL);
            }
        }


        private void OnTimedEvent(object source)
        {
            //Debug.WriteLine("Send keep alive packet {0}", e.SignalTime);
            /*lock (_link.SendData_Lock)
            {
                if (_link.SendDataPortalArrayOUTPUT[0] != null) _link.SendDataPortalArrayOUTPUT[0].Enqueue(_timeoutpacket);
            }
            QueueSend();*/
        }


        internal void QueueStreamDecrypt()
        {
            lock (_IsInQueue_Lock)
            {
                if (IsInQueue == false)
                {
                    IsInQueue = true;
                    ThreadPool.RunAsync(ThreadPoolCallback);
                }
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
                    ThreadPool.RunAsync(SendNotEncrypted);
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
                    ThreadPool.RunAsync(SendKerberos);
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
                    ThreadPool.RunAsync(SendSSL);
                }
            }
        }

        internal static void QueueOnEmptyBuffer()
        {
            if (IsInOnEmptyBufferQueue == false)
            {
                IsInOnEmptyBufferQueue = true;
                ThreadPool.RunAsync(WorkOnEmptyBuffer);
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

            if (_aTimer != null)
            {
                _aTimer.Dispose();
                _aTimer = null;
            }

            lock (_ReceiveDisposelock)
            {
                lock (_SendDisposelock)
                {

                    if (_SocketTCPClient != null)
                    {

                        try
                        {
                            inStream.Dispose();
                        }
                        catch
                        {

                        }
                        try
                        {
                            outStream.Dispose();
                        }
                        catch
                        {

                        }
                        try
                        {
                            _SocketTCPClient.Dispose();
                        }
                        catch
                        {

                        }

                        _SocketTCPClient = null;
                    }

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
                    catch
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
                    catch
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
                    catch
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


        internal async void ReceiveNotEncrypted(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await inStream.ReadAsync(_buff, 0, _buff.Length);
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

        internal async void ReceiveSSL(object state)
        {
            try
            {
                while (true)
                {
                    bytesRead = await inStream.ReadAsync(_buff, 0, _buff.Length);
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

                    bytesRead = await inStream.ReadAsync(_buff, 0, _buff.Length);
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

        internal async void SendNotEncrypted(Object threadContext)
        {
            if (!BootupDone) throw new Exception("Data was send b4 connection was booted.");
            try
            {

                while (StreamIsConnected)
                {
                    if (GetPackets())
                    {
                        await outStream.StoreAsync();
                        return;
                    }

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient != null)
                    {
                        outStream.WriteBytes(byteData._SendData);
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
                    if (GetPackets())
                    {
                        await outStream.StoreAsync();
                        return;
                    }

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient != null)
                    {
                        outStream.WriteBytes(byteData._SendData);
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
                    if (GetPackets())
                    {
                        await outStream.StoreAsync();
                        return;
                    }

                    if (!StreamIsConnected) return;

                    if (_SocketTCPClient != null)
                    {
                        outStream.WriteBytes(byteData._SendData);
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