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


        private SemaphoreSlim _Connection_Lock = new SemaphoreSlim(1);
        public SemaphoreSlim SendData_Lock = new SemaphoreSlim(1);

        private Connection _Connect;
        private bool _Valid = false;

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
        

        private MemoryStream _ms = null;
        public BinaryWriter bw = null;

        public int MajorVersion = 0;
        public int MinorVersion = 0;
        public int BuildVersion = 0;
        public int ProtocolVersion = 0;
        public string TypeString = "CLIENT";
        public string LoginName = "anonymous";
        public string LoginPassword = "anonymous";


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

        public void Accept()
        {
            Valid = true;

            _Static_ThreadLock.Wait();
            _LinkList.Add(this);
            _Static_ThreadLock.Release();
        }

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

            //Connect.Dispose();
        }

        public void SendData()
        {
            SendData_Lock.Wait();
            if (Connect != null && _ms != null)
            {
                if (_ms.Length > 0)
                {
                    //Console.WriteLine("Link.SendData byte wirtten: " + _ms.Length);
                    Connect.SendData(_ms.ToArray());
                    _ms.SetLength(0);
                }
            }
            SendData_Lock.Release();
        }
    }
}
