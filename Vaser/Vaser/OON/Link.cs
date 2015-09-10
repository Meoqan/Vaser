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

        private SemaphoreSlim _Data_Lock = new SemaphoreSlim(1);
        private SemaphoreSlim _Connection_Lock = new SemaphoreSlim(1);
        internal SemaphoreSlim SendData_Lock = new SemaphoreSlim(1);

        private Connection _Connect;
        private bool _Valid = false;
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
                _Data_Lock.Wait();
                string ret = _UserName;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _UserName = value;
                _Data_Lock.Release();
            }
        }

        public bool IsKerberos
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsKerberos;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsKerberos = value;
                _Data_Lock.Release();
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsAuthenticated;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsAuthenticated = value;
                _Data_Lock.Release();
            }
        }

        public bool IsEncrypted
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsEncrypted;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsEncrypted = value;
                _Data_Lock.Release();
            }
        }

        public bool IsMutuallyAuthenticated
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsMutuallyAuthenticated;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsMutuallyAuthenticated = value;
                _Data_Lock.Release();
            }
        }

        public bool IsSigned
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsSigned;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsSigned = value;
                _Data_Lock.Release();
            }
        }

        public bool IsServer
        {
            get
            {
                _Data_Lock.Wait();
                bool ret = _IsServer;
                _Data_Lock.Release();
                return ret;
            }
            set
            {
                _Data_Lock.Wait();
                _IsServer = value;
                _Data_Lock.Release();
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
                _Connection_Lock.Wait();
                Connection ret = _Connect;
                _Connection_Lock.Release();
                return ret;
            }
            set
            {
                _Connection_Lock.Wait();
                _Connect = value;
                _Connection_Lock.Release();
            }
        }

        public bool Valid
        {
            get
            {
                _Connection_Lock.Wait();
                bool ret = _Valid;
                _Connection_Lock.Release();
                return ret;
            }
            set
            {
                _Connection_Lock.Wait();
                _Valid = value;
                _Connection_Lock.Release();
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
            _ms = new MemoryStream();
            bw = new BinaryWriter(_ms);
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

            SendData_Lock.Wait();
            if (bw != null) bw.Dispose();
            if (_ms != null) _ms.Dispose();
            if (bw != null) bw = null;
            if (_ms != null) _ms = null;
            SendData_Lock.Release();

            Connect.Dispose();
        }

        internal void SendData()
        {
            SendData_Lock.Wait();
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
                        GC.Collect();
                    }
                }
            }
            SendData_Lock.Release();
        }
    }
}
