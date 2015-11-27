using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vaser;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;

namespace VaserUnitTest
{
    [TestClass]
    public class LongTimeTest
    {
        [TestMethod]
        public void LongTime()
        {
            Thread srv = new Thread(Server.start);
            srv.Start();


            Thread cli = new Thread(Client.start);
            cli.Start();

            // Use the Join method to block the current thread 
            // until the object's thread terminates.
            cli.Join();
            srv.Join();
            lock(threadlock)
            {
                Assert.AreEqual(true, (stop == true && error == false));
            }
        }
        public static Portal system;
        public static bool stop = false;
        public static bool error = false;
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
        static Portal system = null;
        // create new container
        static TestContainer con1 = new TestContainer();
        static TestContainer con2 = new TestContainer();

        public static void start()
        {
            try
            {
                List<Link> Linklist = new List<Link>();

                List<Link> Removelist = new List<Link>();



                bool online = true;

                


                Thread.Sleep(1000);

                system = LongTimeTest.system;

                System.Timers.Timer _aTimer = new System.Timers.Timer(1);
                _aTimer.Elapsed += DoPackets;
                _aTimer.AutoReset = true;
                _aTimer.Enabled = true;

                //arbeiten
                while (online)
                {
                    lock (LongTimeTest.threadlock)
                    {
                        if (!LongTimeTest.stop)
                        {

                            while (Linklist.Count < 500)
                            {
                                Link lnk1 = VaserClient.ConnectClient("localhost", 3100, VaserOptions.ModeKerberos);
                                Link lnk2 = VaserClient.ConnectClient("localhost", 3101, VaserOptions.ModeKerberos);

                                if (lnk1 != null)
                                {
                                    //Console.WriteLine("1: successfully established connection.");
                                    Linklist.Add(lnk1);
                                }
                                if (lnk2 != null)
                                {
                                    //Console.WriteLine("2: successfully established connection.");
                                    Linklist.Add(lnk2);
                                }



                            }
                        }
                    }
                    /*//send data
                    con1.test = "Data Send.";
                    // the last 2 digits are manually set [1]
                    system.SendContainer(lnk1, con1, 1, 1);
                    con1.test = "Data Send.";
                    system.SendContainer(lnk2, con1, 1, 1);

                    Portal.Finialize();
                    */



                    Thread.Sleep(1);

                    foreach (Link l in Linklist)
                    {
                        lock (LongTimeTest.threadlock)
                        {
                            if (!LongTimeTest.stop)
                            {
                                if (!l.IsConnected) Removelist.Add(l);
                            }
                            else
                            {
                                Removelist.Add(l);
                                online = false;
                            }
                        }
                    }

                    foreach (Link l in Removelist)
                    {
                        Linklist.Remove(l);
                        //free all resources
                        l.Dispose();
                        //Console.WriteLine("CLX DIS");
                    }
                    Removelist.Clear();


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

        private static void DoPackets(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //proceed incoming data
                foreach (Packet_Recv pak in system.GetPakets())
                {
                    switch (pak.ContainerID)
                    {
                        case 1:

                            // [1] now you can sort the packet to the right container and object
                            //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                            //unpack the packet, true if the decode was successful
                            if (con1.UnpackContainer(pak, system))
                            {
                                if (con1.ID < 250)
                                {
                                    con1.ID += 1;
                                    system.SendContainer(pak.link, con1, 1, 1);
                                }
                                else
                                {
                                    Console.WriteLine("Disconnecting!");
                                    pak.link.Dispose();
                                    counter++;
                                    if (counter > 1000)
                                    {
                                        lock (LongTimeTest.threadlock)
                                        {
                                            LongTimeTest.stop = true;
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                Portal.Finialize();
            }
            catch (Exception es)
            {
                lock (LongTimeTest.threadlock)
                {
                    LongTimeTest.error = true;
                    LongTimeTest.stop = true;
                }
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

        static Portal system = null;
        // create new container
        static TestContainer con1 = new TestContainer();
        static TestContainer con2 = new TestContainer();


        public static void start()
        {
            try
            {

                bool online = true;

                //initialize the server
                LongTimeTest.system = new Portal();
                system = LongTimeTest.system;

                //start the server
                VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, VaserOptions.ModeKerberos);
                VaserServer Server2 = new VaserServer(System.Net.IPAddress.Any, 3101, VaserOptions.ModeKerberos);

                //create connection managing lists
                List<Link> Livinglist = new List<Link>();
                List<Link> Removelist = new List<Link>();

                System.Timers.Timer _aTimer = new System.Timers.Timer(1);
                _aTimer.Elapsed += DoPackets;
                _aTimer.AutoReset = true;
                _aTimer.Enabled = true;

                //run the server
                while (online)
                {
                    //accept new client
                    Link lnk1 = Server1.GetNewLink();
                    if (lnk1 != null)
                    {
                        //Console.WriteLine("CL1 CON");
                        Livinglist.Add(lnk1);
                        lnk1.Accept();

                        //send data
                        con1.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
                        // the last 2 digits are manually set [1]
                        system.SendContainer(lnk1, con1, 1, 1);
                    }

                    //accept new client
                    Link lnk2 = Server2.GetNewLink();
                    if (lnk2 != null)
                    {
                        //Console.WriteLine("CL2 CON");
                        Livinglist.Add(lnk2);
                        lnk2.Accept();

                        //send data
                        con1.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
                        // the last 2 digits are manually set [1]
                        system.SendContainer(lnk2, con1, 1, 1);
                    }
                    Portal.Finialize();



                    //Thread.Sleep(10);

                    //disconnet clients
                    foreach (Link l in Livinglist)
                    {
                        lock (LongTimeTest.threadlock)
                        {
                            if (!LongTimeTest.stop)
                            {
                                if (!l.IsConnected) Removelist.Add(l);
                            }
                            else
                            {
                                Removelist.Add(l);
                                online = false;
                            }
                        }
                    }

                    foreach (Link l in Removelist)
                    {
                        Livinglist.Remove(l);
                        //free all resources
                        l.Dispose();
                        //Console.WriteLine("CLX DIS");
                    }
                    Removelist.Clear();

                    Thread.Sleep(1);
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


        private static void DoPackets(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {

                //proceed incoming data
                foreach (Packet_Recv pak in system.GetPakets())
                {
                    // [1] now you can sort the packet to the right container and object
                    //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con1.UnpackContainer(pak, system))
                    {
                        //Console.WriteLine(con1.test);

                        // the last 2 digits are manually set [1]
                        system.SendContainer(pak.link, con1, 1, 1);
                    }
                }

                //send all bufferd data to the clients
                Portal.Finialize();
            }
            catch (Exception xe)
            {
                lock (LongTimeTest.threadlock)
                {
                    LongTimeTest.error = true;
                    LongTimeTest.stop = true;
                }
            }
        }
    }
}
