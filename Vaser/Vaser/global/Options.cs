using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Vaser
{
    internal class Options
    {
        public static volatile bool Operating = true;
        public static readonly int MaximumPacketSize = 65007;
        public static readonly int PacketHeadSize = 7;
        public static int LinkSendBufferSize = 1024;

    }
}
