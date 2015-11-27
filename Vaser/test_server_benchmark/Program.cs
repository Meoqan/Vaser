using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using System.Threading;

namespace test_server_benchmark
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

        static Portal system = null;
        // create new container
        static TestContainer con1 = new TestContainer();
        


        static void Main(string[] args)
        {
            

            bool online = true;

            //initialize the server
            system = new Portal();
            //start the server
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, VaserOptions.ModeKerberos);
            VaserServer Server2 = new VaserServer(System.Net.IPAddress.Any, 3101, VaserOptions.ModeKerberos);
            TestContainer con2 = new TestContainer();
            //create connection managing lists
            List<Link> Livinglist = new List<Link>();
            List<Link> Removelist = new List<Link>();

            System.Timers.Timer _aTimer = new System.Timers.Timer(10);
            _aTimer.Elapsed += DoPackets;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
            int object_counter = 0;
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
                    con1.ID = 0;
                    con1.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
                    // the last 2 digits are manually set [1]
                    object_counter++;
                    system.SendContainer(lnk1, con1, 1, object_counter);
                }

                //accept new client
                Link lnk2 = Server2.GetNewLink();
                if (lnk2 != null)
                {
                    //Console.WriteLine("CL2 CON");
                    Livinglist.Add(lnk2);
                    lnk2.Accept();

                    //send data
                    con1.ID = 0;
                    con1.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
                    // the last 2 digits are manually set [1]
                    object_counter++;
                    system.SendContainer(lnk2, con1, 1, object_counter);
                }
                Portal.Finialize();

                
                //proceed incoming data
                foreach (Packet_Recv pak in system.GetPakets())
                {
                    // [1] now you can sort the packet to the right container and object
                    //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con2.UnpackContainer(pak, system))
                    {
                        //Console.WriteLine(con1.test);
                        //Console.WriteLine("Pong!  CounterID" + con2.ID + " Object:" + pak.ObjectID);
                        // the last 2 digits are manually set [1]
                        system.SendContainer(pak.link, con2, 1, pak.ObjectID);
                    }
                    else
                    {
                        Console.WriteLine("Decode error");
                    }
                }

                //send all bufferd data to the clients
                Portal.Finialize();

                //Thread.Sleep(10);

                //disconnet clients
                foreach (Link l in Livinglist)
                {
                    //con2.test = "beep.";
                    //con2.array = new int[1];
                    //system.SendContainer(l, con2, 1, 1);
                    //Console.WriteLine("beep.");
                    if (!l.IsConnected) Removelist.Add(l);
                }

                foreach(Link l in Removelist)
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


        private static void DoPackets(Object source, System.Timers.ElapsedEventArgs e)
        {
            
        }
    }
}
