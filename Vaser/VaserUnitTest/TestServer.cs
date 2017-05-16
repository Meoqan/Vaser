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
    public class TestServer
    {
        public PortalCollection PCollS = null;
        public Portal testS = null;
        public VaserServer RunningServer = null;
        public short Port = 3120;
        public AutoResetEvent autoEvent = new AutoResetEvent(false);

        public void InitPortals()
        {
            testS = new Portal(100);
            PCollS = new PortalCollection();
            PCollS.RegisterPortal(testS);
        }

        public void DelegatePortals()
        {
            RunningServer.NewLink += OnNewLinkRunningServer;
            RunningServer.DisconnectingLink += OnDisconnectingLinkRunningServer;

            testS.IncomingPacket += OnTestRunningServer;
        }

        public void InitServer()
        {
            RunningServer = new VaserServer(System.Net.IPAddress.Any, Port, PCollS);
        }

        public void StartServer()
        {
            if (RunningServer != null) RunningServer.Start();
        }

        public void StopServer()
        {
            if(RunningServer != null) RunningServer.Stop();
        }

        public static TestServer CreateServer()
        {
            TestServer Server = new TestServer();


            return Server;
        }

        TestContainer con3 = new TestContainer();
        void OnNewLinkRunningServer(object p, LinkEventArgs e)
        {
            e.lnk.Accept();

            //send data
            con3.ID = 0;
            con3.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
            // the last 2 digits are manually set [1]
            testS.SendContainer(e.lnk, con3, 1, 0);

        }

        TestContainer con2 = new TestContainer();
        void OnTestRunningServer(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con2.UnpackContainer(e.pak))
            {
                //Console.WriteLine(con1.test);
                Debug.WriteLine("Ping!  CounterID" + con2.ID + " Object:" + e.pak.ObjectID);
                // the last 2 digits are manually set [1]
                e.portal.SendContainer(e.lnk, con2, 1, e.pak.ObjectID);
            }
            else
            {
                Console.WriteLine("Decode error");
            }
        }

        void OnDisconnectingLinkRunningServer(object p, LinkEventArgs e)
        {
            Console.WriteLine("CL1 DIS");
            autoEvent.Set();
        }
    }
}
