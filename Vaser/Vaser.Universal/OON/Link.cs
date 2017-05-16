using System;
using System.Collections.Generic;
using System.Net;

namespace Vaser
{
    /// <summary>
    /// This class manages your connections to the server.
    /// </summary>
    public class Link
    {
        //internal static object _Static_ThreadLock = new object();
        //private static List<Link> _LinkList = new List<Link>();

        //private object _Data_Lock = new object();
        private object _Connection_Lock = new object();
        internal object SendData_Lock = new object();

        //private Connection _Connect;
        internal volatile bool Valid = false;
        private volatile bool Teardown = false;

        /// <summary>
        /// Indicates if the Link was disposed.
        /// </summary>
        public volatile bool Disposed;
        private object _DisposeLock = new object();

        /// <summary>
        /// Input sending data from portal
        /// </summary>
        internal Queue<Packet_Send>[] SendDataPortalArray = new Queue<Packet_Send>[256];

        /// <summary>
        /// merged mapping for sending (removed empty spaces for reduced QoS operations)
        /// </summary>
        internal Queue<Packet_Send>[] SendDataPortalArrayOUTPUT = null;

        /// <summary>
        /// EventHandler for disconnecting
        /// </summary>
        public event EventHandler<LinkEventArgs> Disconnecting;

        /// <summary>
        /// EventHandler for empty buffer
        /// triggers an event from when the buffer is empty
        /// </summary>
        public event EventHandler<LinkEventArgs> EmptyBuffer;

        /// <summary>
        /// Any to this link related object can be safed here. This variable is threadsafe and is free to use.
        /// </summary>
        public object AttachedObject
        {
            get;
            set;
        }

        /// <summary>
        /// Contains the reference of the server. Is null on the client side.
        /// </summary>
        public VaserServer vServer
        {
            get;
            internal set;
        }

        /// <summary>
        /// Do not use. Used by vContentDelivery
        /// </summary>
        public object vCDObject
        {
            get;
            set;
        }

        /// <summary>
        /// Any to this link related ID can be safed here. This variable is threadsafe and is free to use.
        /// </summary>
        public uint AttachedID
        {
            get;
            set;
        }

        /// <summary>
        /// Contains the UserName of Kerberos connections.
        /// </summary>
        public string UserName
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link uses kerberos.
        /// </summary>
        public bool IsKerberos
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link is encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link is full authenticated and trusted.
        /// </summary>
        public bool IsMutuallyAuthenticated
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link is signed.
        /// </summary>
        public bool IsSigned
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates if the link is from a server.
        /// </summary>
        public bool IsServer
        {
            get;
            internal set;
        }

        internal Connection Connect
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates if the link is connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Connect.StreamIsConnected;
            }
        }


        /// <summary>
        /// the remote endpoint IPAddress
        /// </summary>
        public Windows.Networking.HostName IPv4Address
        {
            get
            {
                return Connect.IPv4Address;
            }
        }

        /// <summary>
        /// Do not create links of your own.
        /// </summary>
        /// <param name="Pcol">The PortalCollection.</param>
        public Link(PortalCollection Pcol)
        {
            lock (SendData_Lock)
            {
                int counter = 0;
                for (int x = 0; x < 256; x++)
                {
                    if (Pcol.PortalArray[x] != null) counter++;
                }
                //Debug.WriteLine("Portal counter is "+ counter);
                SendDataPortalArrayOUTPUT = new Queue<Packet_Send>[counter];

                counter = 0;
                for (int x = 0; x < 256; x++)
                {
                    if (Pcol.PortalArray[x] != null)
                    {
                        SendDataPortalArray[x] = new Queue<Packet_Send>();

                        SendDataPortalArrayOUTPUT[counter] = SendDataPortalArray[x];
                        //Debug.WriteLine("Mapping Portal ID from "+x+" to "+ counter);
                        counter++;
                    }
                }
            }
        }

        /// <summary>
        /// Accept the new connected client. Incoming data will be now received.
        /// </summary>
        public void Accept()
        {
            Valid = true;

            if (Connect._IsAccepted == false)
            {

                Connect.AcceptConnection();

            }
        }

        /// <summary>
        /// Raises an event if the link is disconnected.
        /// </summary>
        /// <param name="e">The link connection.</param>
        protected virtual void OnDisconnectingLink(LinkEventArgs e)
        {

            Disconnecting?.Invoke(this, e);
        }

        /// <summary>
        /// Raises an event if the Buffer of this link is empty. The 'OnEmptybuffer' parameter must set when a packet is send.
        /// </summary>
        /// <param name="e">The link connection.</param>
        protected internal virtual void OnEmptyBuffer(LinkEventArgs e)
        {

            EmptyBuffer?.Invoke(this, e);
        }


        /// <summary>
        /// Close the connection and free all resources.
        /// </summary>
        public void Dispose()
        {
            //Debug.WriteLine("Link.Dispose called");
            lock (_DisposeLock)
            {
                //SendData();
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
            }

            Connect.Dispose();

            /*lock (_Static_ThreadLock)
            {
                if (LinkList.Contains(this)) LinkList.Remove(this);
            }*/

            if (Connect.server != null) Connect.server.RemoveFromConnectionList(Connect);
            lock (SendData_Lock)
            {
                for (int x = 0; x < SendDataPortalArray.Length; x++)
                {
                    if (SendDataPortalArray[x] != null)
                    {
                        SendDataPortalArray[x].Clear();
                        SendDataPortalArray[x] = null;
                    }
                }

                for (int x = 0; x < SendDataPortalArrayOUTPUT.Length; x++)
                {
                    SendDataPortalArrayOUTPUT[x] = null;
                }
            }

            Connect._PCollection.RemoveDisconectingLinkFromRequest(this);

            if (!Teardown)
            {
                Teardown = true;
                LinkEventArgs args = new LinkEventArgs()
                {
                    lnk = this
                };
                OnDisconnectingLink(args);
            }

            //Debug.WriteLine("Link.Dispose ended");
        }

    }
}
