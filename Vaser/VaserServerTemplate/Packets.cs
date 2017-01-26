using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;

namespace VaserServerTemplate
{
    public class TestContainer : Container
    {
        //only public, nonstatic and standard datatypes can be transmitted
        //maximum packetsize is 65000 bytes
        internal const ushort ID = 100;

        public string MyText = "Hello World!";
    }
}
