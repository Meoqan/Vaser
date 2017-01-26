using System;
using Vaser;
using Vaser.OON;

namespace test_client
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
                Console.WriteLine(con2.test);

            }
            else
            {
                Console.WriteLine("Decode error!");
            }
        }

    }
}
