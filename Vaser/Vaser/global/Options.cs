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
        private static SemaphoreSlim _Static_ThreadLock = new SemaphoreSlim(1);
        private static bool _Operating = true;
        private static int _MaximumPacketSize = 65012;
        private static int _PacketHeadSize = 12;

        public static bool Operating
        {
            get
            {
                _Static_ThreadLock.Wait();
                bool ret = _Operating;
                _Static_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Static_ThreadLock.Wait();
                _Operating = value;
                _Static_ThreadLock.Release();
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
        }
    }
}
