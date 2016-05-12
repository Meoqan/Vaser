using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using System.Diagnostics;
using System.Threading;

namespace VaserUnitTest
{
    public class TestClient
    {
        public PortalCollection PCollC = null;
        public Portal testC = null;
        public Link lnkC = null;
        public short Port = 3120;

        public static TestClient CreateClient()
        {
            TestClient client = new TestClient();
            return client;
        }

        public void InitPortals()
        {
            testC = new Portal(100);
            PCollC = new PortalCollection();
            PCollC.RegisterPortal(testC);
        }
        public void DelegatePortals()
        {
            testC.IncomingPacket += OnTestClient;
        }

        public void ConnectClient()
        {
            lnkC = VaserClient.ConnectClient("localhost", Port, PCollC);
            if (lnkC != null) Console.WriteLine("1: successfully established connection.");
        }

        public void DisconnectClient()
        {
            if (lnkC != null) lnkC.Dispose();
        }

        TestContainer con4 = new TestContainer();
        void OnTestClient(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con4.UnpackContainer(e.pak, e.portal))
            {
                //Console.WriteLine(con1.test);
                Debug.WriteLine("Pong!  CounterID" + con4.ID + " Object:" + e.pak.ObjectID);
                e.lnk.Dispose();
            }
            else
            {
                Debug.WriteLine("Decode error");
            }
        }
    }
}
