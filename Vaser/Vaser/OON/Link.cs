using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;

namespace Vaser
{
    /// <summary>
    /// This class manages your connections to the server.
    /// </summary>
    public class Link
    {
        internal static object _Static_ThreadLock = new object();
        private static List<Link> _LinkList = new List<Link>();

        private object _Data_Lock = new object();
        private object _Connection_Lock = new object();
        internal object SendData_Lock = new object();

        private Connection _Connect;
        internal volatile bool Valid = false;
        private volatile bool Teardown = false;
        private MemoryStream _ms = null;
        internal BinaryWriter bw = null;

        private object _AttachedObject = null;
        private uint _AttachedID = 0;

        //Kerberos
        private string _UserName = string.Empty;

        private bool _IsKerberos = false;
        private bool _IsAuthenticated = false;
        private bool _IsEncrypted = false;
        private bool _IsMutuallyAuthenticated = false;
        private bool _IsSigned = false;
        private bool _IsServer = false;

        /// <summary>
        /// EventHandler for disconnecting
        /// </summary>
        public event EventHandler<LinkEventArgs> Disconnecting;


        /// <summary>
        /// Any to this link related object can be safed here. This variable is threadsafe and is free to use.
        /// </summary>
        public object AttachedObject
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _AttachedObject;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _AttachedObject = value;
                }
            }
        }

        /// <summary>
        /// Any to this link related ID can be safed here. This variable is threadsafe and is free to use.
        /// </summary>
        public uint AttachedID
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _AttachedID;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _AttachedID = value;
                }
            }
        }

        /// <summary>
        /// Contains the UserName of Kerberos connections.
        /// </summary>
        public string UserName
        {
            get
            {
                lock(_Data_Lock)
                {
                    return _UserName;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _UserName = value;
                }
            }
        }

        public bool IsKerberos
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsKerberos;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsKerberos = value;
                }
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsAuthenticated;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsAuthenticated = value;
                }
            }
        }

        public bool IsEncrypted
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsEncrypted;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsEncrypted = value;
                }
            }
        }

        public bool IsMutuallyAuthenticated
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsMutuallyAuthenticated;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsMutuallyAuthenticated = value;
                }
            }
        }

        public bool IsSigned
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsSigned;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsSigned = value;
                }
            }
        }

        public bool IsServer
        {
            get
            {
                lock (_Data_Lock)
                {
                    return _IsServer;
                }
            }
            set
            {
                lock (_Data_Lock)
                {
                    _IsServer = value;
                }
            }
        }

        /// <summary>
        /// A list of all active links. Do not modify!
        /// </summary>
        public static List<Link> LinkList
        {
            get
            {
                lock(_Static_ThreadLock)
                {
                    return _LinkList;
                }
            }
            set
            {
                lock (_Static_ThreadLock)
                {
                    _LinkList = value;
                }
            }
        }

        internal Connection Connect
        {
            get
            {
                lock (_Connection_Lock)
                {
                    return _Connect;
                }
            }
            set
            {
                lock (_Connection_Lock)
                {
                    _Connect = value;
                }
            }
        }

        /*public bool Valid
        {
            get
            {
                lock (_Connection_Lock)
                {
                    return _Valid;
                }
            }
            set
            {
                lock (_Connection_Lock)
                {
                    _Valid = value;
                }
            }
        }*/

        public bool IsConnected
        {
            get
            {
                return _Connect.StreamIsConnected;
            }
        }


        /// <summary>
        /// the remote endpoint IPAddress
        /// </summary>
        public IPAddress IPv4Address
        {
            get
            {
                return _Connect.IPv4Address;
            }
        }

        public Link()
        {
            lock (SendData_Lock)
            {
                _ms = new MemoryStream();
                bw = new BinaryWriter(_ms);
            }
        }

        /// <summary>
        /// Accept the new connected client. Incoming data will be now received.
        /// </summary>
        public void Accept()
        {
            Valid = true;

            lock (_Static_ThreadLock)
            {
                _LinkList.Add(this);

            }
        }

        internal static void SendAllData()
        {
            lock (_Static_ThreadLock)
            {
                foreach (Link lnk in _LinkList)
                {
                    lnk.SendData();
                }
            }
        }

        protected virtual void OnDisconnectingLink(LinkEventArgs e)
        {

            EventHandler<LinkEventArgs> handler = Disconnecting;
            if (handler != null)
            {
                //Console.WriteLine("OnDisconnectingLink called!");
                handler(this, e);
            }
        }


        /// <summary>
        /// Close the connection and free all resources.
        /// </summary>
        public void Dispose()
        {
            SendData();

            Connect.Stop();

            lock (_Static_ThreadLock)
            {
                if (_LinkList.Contains(this)) _LinkList.Remove(this);
            }

            lock (SendData_Lock)
            {
                if (bw != null) bw.Dispose();
                if (_ms != null) _ms.Dispose();
                if (bw != null) bw = null;
                if (_ms != null) _ms = null;
            }

            Connect.Dispose();

            if (!Teardown)
            {
                
                Teardown = true;
                LinkEventArgs args = new LinkEventArgs();
                args.lnk = this;
                OnDisconnectingLink(args);
            }

            
        }

        

        internal void SendData()
        {
            lock (SendData_Lock)
            {
                if (Connect != null && _ms != null)
                {
                    if (_ms.Length > 0)
                    {
                        //Debug.WriteLine("Link.SendData byte wirtten: " + _ms.Length);
                        Connect.SendData(_ms.ToArray());



                        //_ms.SetLength(0);
                        //_ms.Flush();
                        //bw.Flush();

                        if (_ms.Length < 10000000)
                        {
                            _ms.SetLength(0);
                            _ms.Flush();
                            bw.Flush();
                        }
                        else
                        {
                            _ms.Dispose();
                            bw.Dispose();
                            _ms = new MemoryStream();
                            bw = new BinaryWriter(_ms);
                            //GC.Collect();
                        }
                    }
                }
            }
        }
    }
}
