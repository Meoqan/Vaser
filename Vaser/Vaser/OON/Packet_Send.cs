using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    internal class Packet_Send
    {
        internal byte[] _SendData = null;
        internal int Counter = 0;
        internal bool _CallEmpybuffer = false;

        internal Packet_Send(byte[] Data, bool CallEmpybuffer)
        {
            _SendData = Data;
            _CallEmpybuffer = CallEmpybuffer;
        }
    }
}
