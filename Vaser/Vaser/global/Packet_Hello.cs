using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser.global
{
    public class Packet_Hello : Container
    {
        public static Packet_Hello spak = new Packet_Hello();

        public int MajorVersion = 0;
        public int MinorVersion = 0;
        public int BuildVersion = 0;
        public int ProtocolVersion = 0;
        public string TypeString = "CLIENT";
        public string LoginName = "anonymous";
        public string LoginPassword = "anonymous";
    }
}
