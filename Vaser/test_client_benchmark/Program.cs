using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using System.Threading;

namespace test_client_benchmark
{
    class Program
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
        

        static void Main(string[] args)
        {
            List<Link> Linklist = new List<Link>();

            List<Link> Removelist = new List<Link>();

            TestContainer con1 = new TestContainer();
            TestContainer con2 = new TestContainer();

            bool online = true;

            system = new Portal();


            Thread.Sleep(1000);

            System.Timers.Timer _aTimer = new System.Timers.Timer(10);
            _aTimer.Elapsed += DoPackets;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
            //arbeiten

            
            while (online)
            {

                while (Linklist.Count < 5)
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



                /*//send data
                con1.test = "Data Send.";
                // the last 2 digits are manually set [1]
                system.SendContainer(lnk1, con1, 1, 1);
                con1.test = "Data Send.";
                system.SendContainer(lnk2, con1, 1, 1);

                Portal.Finialize();
                */

                
                //proceed incoming data
                foreach (Packet_Recv pak in system.GetPakets())
                {
                    switch (pak.ContainerID)
                    {
                        case 1:

                            // [1] now you can sort the packet to the right container and object
                            //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                            //unpack the packet, true if the decode was successful
                            if (con2.UnpackContainer(pak, system))
                            {
                                if (con2.ID < 0) Console.WriteLine("Decode error: " + con2.ID);
                                //if (con2.ID > 100) Console.WriteLine("Decode error: " + con2.ID);
                                if (con2.ID < 5000)
                                {
                                    //Console.WriteLine("Ping! " + counter + " CounterID" + con2.ID + " Object:" + pak.ObjectID);

                                    con2.ID += 1;
                                    system.SendContainer(pak.link, con2, 1, pak.ObjectID);
                                }
                                else
                                {
                                    counter++;
                                    Console.WriteLine("Disconnecting! " + counter + " CounterID" + con2.ID + " Object:" + pak.ObjectID);
                                    pak.link.Dispose();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Decode error");
                            }
                            break;
                    }
                }
                Portal.Finialize();

                Thread.Sleep(1);
                
                foreach(Link l in Linklist)
                {
                    if (!l.IsConnected) Removelist.Add(l);
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

        static int counter = 0;
        private static void DoPackets(Object source, System.Timers.ElapsedEventArgs e)
        {
            
        }
    }
}
