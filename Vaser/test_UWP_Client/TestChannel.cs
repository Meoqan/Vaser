using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using Vaser.OON;
using System.Diagnostics;

namespace test_UWP_Client
{
    public class TestChannel : cChannel
    {
        TestContainer con1 = new TestContainer();
        public void mySendStarter(string myMessage, Link lnk)
        {
            con1.test = myMessage;

            SendPacket(con1, lnk);
        }

        TestContainer con2 = new TestContainer();
        public override void IncomingPacket(object p, PacketEventArgs e)
        {
            if (e.pak != null && con2.UnpackContainer(e.pak, e.portal))
            {
                Debug.WriteLine(con2.test);

            }
            else
            {
                Debug.WriteLine("Decode error!");
            }
        }

    }
}
