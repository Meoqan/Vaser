using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Vaser.global
{
    public class Options
    {
        //private static object _Static_ThreadLock = new object();
        public static volatile bool Operating = true;
        public static readonly int MaximumPacketSize = 65012;
        public static readonly int PacketHeadSize = 12;
        /*
        public static bool Operating
        {
            get
            {
                lock(_Static_ThreadLock)
                {
                    return _Operating;
                }
            }
            set
            {
                lock (_Static_ThreadLock)
                {
                    _Operating = value;
                }
            }
        }
        
        public static int MaximumPacketSize
        {
            get
            {
                _Static_ThreadLock.Wait();
                int ret = _MaximumPacketSize;
                _Static_ThreadLock.Release();
                return ret;
            }
            set
            {
                if (value > 0 && value < Int32.MaxValue - _PacketHeadSize)
                {
                    _Static_ThreadLock.Wait();
                    _MaximumPacketSize = value + _PacketHeadSize;
                    _Static_ThreadLock.Release();
                }
            }
        }

        public static int PacketHeadSize
        {
            get
            {
                _Static_ThreadLock.Wait();
                int ret = _PacketHeadSize;
                _Static_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Static_ThreadLock.Wait();
                _PacketHeadSize = value;
                _Static_ThreadLock.Release();
            }
        }*/
    }
}
