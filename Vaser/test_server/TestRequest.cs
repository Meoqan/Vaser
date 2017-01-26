using System;
using Vaser;
using Vaser.OON;

namespace test_server
{
    public class TestRequest : cRequest
    {
        TestContainer con1 = new TestContainer();
        public override void IncomingRequest(object p, PacketEventArgs e)
        {
            if (e.pak != null && con1.UnpackContainer(e.pak, e.portal))
            {
                Console.WriteLine(con1.test);
                con1.test = "Hello Back!";

                RequestResponse(con1);
            }
            else
            {
                RequestResponse(null);
            }
        }
    }
}
