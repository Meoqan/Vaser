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
                Debug.WriteLine(con2.test);

                SetDone("myResult");
            }
            else
            {
                SetError("Decode error.");
            }
        }

    }
}
