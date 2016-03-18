using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vaser;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace VaserUnitTest
{
    // Build your data container
    public class TestContainer : Container
    {
        //only public, nonstatic and standard datatypes can be transmitted
        public int ID = 1;
        public string test = "test text!";

        //also 1D arrays are posible
        public int[] array = new int[1000];
    }

    [TestClass]
    public class VaserConnection
    {
        [TestMethod]
        [ExpectedException(typeof(SocketException))]
        public void FailConnection()
        {
            PortalCollection PColl = new PortalCollection();
            Portal test = PColl.CreatePortal(0);
            Link lnkC = VaserClient.ConnectClient("localhost", 3110, PColl);
        }

        PortalCollection PCollS = null;
        Portal testS = null;

        PortalCollection PCollC = null;
        Portal testC = null;
        AutoResetEvent autoEvent = new AutoResetEvent(false);

        [TestMethod]
        public void TestServer()
        {
            // create new container
            TestContainer con1 = new TestContainer();

            //initialize the server
            PCollS = new PortalCollection();
            testS = PCollS.CreatePortal(0);

            testS.IncomingPacket += OnTestServer;

            //start the server
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3120, PCollS);

            Server1.NewLink += OnNewLinkServer1;
            Server1.DisconnectingLink += OnDisconnectingLinkServer1;


            PCollC = new PortalCollection();
            testC = PCollC.CreatePortal(0);
            testC.IncomingPacket += OnTestClient;

            Link lnkC = VaserClient.ConnectClient("localhost", 3120, PCollC);


            if (lnkC != null) Console.WriteLine("1: successfully established connection.");


            autoEvent.WaitOne();


            //close the server
            Server1.Stop();


        }


        TestContainer con3 = new TestContainer();
        void OnNewLinkServer1(object p, LinkEventArgs e)
        {
            e.lnk.Accept();

            //send data
            con3.ID = 0;
            con3.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
            // the last 2 digits are manually set [1]
            testS.SendContainer(e.lnk, con3, 1, 0);

        }

        void OnDisconnectingLinkServer1(object p, LinkEventArgs e)
        {
            Console.WriteLine("CL1 DIS");

            autoEvent.Set();
        }

        TestContainer con2 = new TestContainer();
        void OnTestServer(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con2.UnpackContainer(e.pak, e.portal))
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

        TestContainer con4 = new TestContainer();
        void OnTestClient(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con4.UnpackContainer(e.pak, e.portal))
            {
                //Console.WriteLine(con1.test);
                Debug.WriteLine("Pong!  CounterID" + con2.ID + " Object:" + e.pak.ObjectID);
                e.lnk.Dispose();
            }
            else
            {
                Debug.WriteLine("Decode error");
            }
        }
    }
}
