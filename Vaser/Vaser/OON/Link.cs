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
    public class Link
    {
        private static SemaphoreSlim _Static_ThreadLock = new SemaphoreSlim(1);
        private static List<Link> _LinkList = new List<Link>();

        private object _Data_Lock = new object();
        private object _Connection_Lock = new object();
        internal object SendData_Lock = new object();

        private Connection _Connect;
        public volatile bool Valid = false;
        private MemoryStream _ms = null;
        internal BinaryWriter bw = null;

        //Kerberos
        private string _UserName = string.Empty;

        private bool _IsKerberos = false;
        private bool _IsAuthenticated = false;
        private bool _IsEncrypted = false;
        private bool _IsMutuallyAuthenticated = false;
        private bool _IsSigned = false;
        private bool _IsServer = false;


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

        public static List<Link> LinkList
        {
            get
            {
                _Static_ThreadLock.Wait();
                List<Link> ret = _LinkList;
                _Static_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Static_ThreadLock.Wait();
                _LinkList = value;
                _Static_ThreadLock.Release();
            }
        }

        public Connection Connect
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
        /// Accept the client
        /// </summary>
        public void Accept()
        {
            Valid = true;

            _Static_ThreadLock.Wait();
            _LinkList.Add(this);
            
            _Static_ThreadLock.Release();
        }

        /// <summary>
        /// Close the connection and free all resources
        /// </summary>
        public void Dispose()
        {
            SendData();

            Connect.Stop();

            _Static_ThreadLock.Wait();
            if (_LinkList.Contains(this)) _LinkList.Remove(this);
            _Static_ThreadLock.Release();

            lock (SendData_Lock)
            {
                if (bw != null) bw.Dispose();
                if (_ms != null) _ms.Dispose();
                if (bw != null) bw = null;
                if (_ms != null) _ms = null;
            }

            Connect.Dispose();
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
