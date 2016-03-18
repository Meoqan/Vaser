using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vaser;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace VaserUnitTest
{
    [TestClass]
    public class LongTimeTest
    {
        [TestMethod]
        public void LongTime()
        {
            Trace.WriteLine("Starting Test!");

            Thread srv = new Thread(Server.start);
            srv.Start();
            

            Thread cli = new Thread(Client.start);
            cli.Start();

            // Use the Join method to block the current thread 
            // until the object's thread terminates.
            cli.Join();
            srv.Join();
            lock (threadlock)
            {
                Assert.AreEqual(true, (stop == true && error == false));
            }
        }
        public static volatile bool stop = false;
        public static volatile bool error = false;
        public static object threadlock = new object();

    }



    public class Client
    {


        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";

            //also 1D arrays are posible
            //public int[] array = new int[1000];
        }
        //Client initalisieren
        public static VaserKerberosClient KClient = new VaserKerberosClient();

        static PortalCollection PColl = null;
        static Portal system = null;

        static object Livinglist_lock = new object();
        static List<Link> Livinglist = new List<Link>();


        public static void start()
        {
            try
            {

                bool online = true;

                Thread.Sleep(1000);

                PColl = new PortalCollection();
                system = PColl.CreatePortal(100);

                system.IncomingPacket += OnSystemPacket;

                while (online)
                {

                    while (Livinglist.Count < 10)
                    {
                        Link lnk1 = VaserClient.ConnectClient("localhost", 3100, PColl);
                        lnk1.Disconnecting += OnDisconnectingLink;
                        Link lnk2 = VaserClient.ConnectClient("localhost", 3101, PColl);
                        lnk2.Disconnecting += OnDisconnectingLink;

                        if (lnk1 != null)
                        {
                            //Console.WriteLine("1: successfully established connection.");
                            lock (Livinglist_lock)
                            {
                                Livinglist.Add(lnk1);
                            }
                        }
                        else
                        {
                            throw new Exception("Connerror!");
                        }
                        if (lnk2 != null)
                        {
                            //Console.WriteLine("2: successfully established connection.");
                            lock (Livinglist_lock)
                            {
                                Livinglist.Add(lnk2);
                            }
                        }
                        else
                        {
                            throw new Exception("Connerror!");
                        }
                    }

                    Thread.Sleep(1);
                    if(counter > 1000)
                    {
                        LongTimeTest.stop = true;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                lock (LongTimeTest.threadlock)
                {
                    LongTimeTest.error = true;
                    LongTimeTest.stop = true;
                }
            }
        }
        static int counter = 0;

        static void OnDisconnectingLink(object p, LinkEventArgs e)
        {
            Debug.WriteLine("CL1 CON");
            lock (Livinglist_lock)
            {
                Livinglist.Remove(e.lnk);
            }
        }

        static TestContainer con2 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            //Debug.WriteLine("New Packet in Client");
            switch (e.pak.ContainerID)
            {
                case 1:

                    // [1] now you can sort the packet to the right container and object
                    //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con2.UnpackContainer(e.pak, e.portal))
                    {
                        if (con2.ID < 0) throw new Exception("Decode error: " + con2.ID);
                        //if (con2.ID > 100) Console.WriteLine("Decode error: " + con2.ID);
                        if (con2.ID < 500)
                        {
                            //Console.WriteLine("Ping! " + counter + " CounterID" + con2.ID + " Object:" + pak.ObjectID);

                            con2.ID += 1;
                            e.portal.SendContainer(e.pak.link, con2, 1, e.pak.ObjectID);
                        }
                        else
                        {
                            counter++;
                            Debug.WriteLine("Disconnecting! " + counter + " CounterID" + con2.ID + " Object:" + e.pak.ObjectID);
                            e.pak.link.Dispose();
                        }
                    }
                    else
                    {
                        throw new Exception("Decode error");
                    }
                    break;
                default:
                    throw new Exception("wrong con ID");
            }
        }
    }
    



    public class Server
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";

            //also 1D arrays are posible
            //public int[] array = new int[1000];
        }

        public static VaserKerberosServer KServer = new VaserKerberosServer();

        static PortalCollection PColl = null;
        static Portal system = null;

        //create connection managing lists
        static object Livinglist_lock = new object();
        static List<Link> Livinglist = new List<Link>();
        static uint object_counter = 0;

        public static void start()
        {
            try
            {

                //initialize the server
                PColl = new PortalCollection();
                system = PColl.CreatePortal(100);

                system.IncomingPacket += OnSystemPacket;

                //start the server
                VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, PColl);
                VaserServer Server2 = new VaserServer(System.Net.IPAddress.Any, 3101, PColl);

                Server1.NewLink += OnNewLinkServer1;
                Server2.NewLink += OnNewLinkServer2;

                Server1.DisconnectingLink += OnDisconnectingLinkServer1;
                Server2.DisconnectingLink += OnDisconnectingLinkServer2;

                //run the server
                while(!LongTimeTest.stop)
                {
                    Thread.Sleep(1000);
                }

                //close the server
                Server1.Stop();
                Server2.Stop();
            }
            catch (Exception e)
            {
                lock (LongTimeTest.threadlock)
                {
                    LongTimeTest.error = true;
                    LongTimeTest.stop = true;
                }
            }
        }

        static TestContainer con3 = new TestContainer();
        static void OnNewLinkServer1(object p, LinkEventArgs e)
        {

            Debug.WriteLine("CL1 CON");
            lock (Livinglist_lock)
            {
                Livinglist.Add(e.lnk);
            }
            e.lnk.Accept();

            //send data
            con3.ID = 0;
            con3.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
            // the last 2 digits are manually set [1]
            object_counter++;
            system.SendContainer(e.lnk, con3, 1, object_counter);

        }

        static TestContainer con4 = new TestContainer();
        static void OnNewLinkServer2(object p, LinkEventArgs e)
        {

            Debug.WriteLine("CL2 CON");
            lock (Livinglist_lock)
            {
                Livinglist.Add(e.lnk);
            }
            e.lnk.Accept();

            //send data
            con4.ID = 0;
            con4.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
            // the last 2 digits are manually set [1]
            object_counter++;
            system.SendContainer(e.lnk, con4, 1, object_counter);

        }

        static void OnDisconnectingLinkServer1(object p, LinkEventArgs e)
        {
            //Console.WriteLine("CL1 DIS");
            lock (Livinglist_lock)
            {
                Livinglist.Remove(e.lnk);
            }
        }

        static void OnDisconnectingLinkServer2(object p, LinkEventArgs e)
        {
            //Console.WriteLine("CL2 DIS");
            lock (Livinglist_lock)
            {
                Livinglist.Remove(e.lnk);
            }
        }

        static TestContainer con2 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            //Debug.WriteLine("New Packet in Server");
            //unpack the packet, true if the decode was successful
            if (con2.UnpackContainer(e.pak, e.portal))
            {
                //Console.WriteLine(con1.test);
                //Console.WriteLine("Pong!  CounterID" + con2.ID + " Object:" + pak.ObjectID);
                // the last 2 digits are manually set [1]
                e.portal.SendContainer(e.lnk, con2, 1, e.pak.ObjectID);
            }
            else
            {
                throw new Exception("Decode error");
            }
        }
    }
}
