using System;
using Vaser;
using Vaser.OON;

namespace test_client
{
    public class TestRequest : cRequest
    {
        TestContainer con1 = new TestContainer();
        public cStatus myRequestStarter(string myMessage, Link lnk)
        {
            con1.test = myMessage;

            return StartRequest(lnk, con1);
        }

        TestContainer con2 = new TestContainer();
        public override void RequestResult(object p, PacketEventArgs e)
        {
            if (con2.UnpackContainer(e.pak))
            {
                Console.WriteLine(con2.test);

                SetDone("myResult");
            }
            else
            {
                SetError("Decode error.");
            }
        }

    }
}
