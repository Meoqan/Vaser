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

        //static Portal system = null;
        // create new container
        //static TestContainer con1 = new TestContainer();

        //create connection managing lists
        static object Livinglist_lock = new object();
        static List<Link> Livinglist = new List<Link>();

        static Portal system = null;
        static uint object_counter = 0;

        static void Main(string[] args)
        {
            //Client initalisieren
            PortalCollection PC = new PortalCollection();
            system = PC.CreatePortal(100);

            system.IncomingPacket += OnSystemPacket;

            //start the server
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, VaserOptions.ModeNotEncrypted, PC);
            VaserServer Server2 = new VaserServer(System.Net.IPAddress.Any, 3101, VaserOptions.ModeNotEncrypted, PC);

            Server1.NewLink += OnNewLinkServer1;
            Server2.NewLink += OnNewLinkServer2;

            Server1.DisconnectingLink += OnDisconnectingLinkServer1;
            Server2.DisconnectingLink += OnDisconnectingLinkServer2;


            //TestContainer con2 = new TestContainer();




            //run the server
            Console.ReadKey();

            //close the server
            Server1.Stop();
            Server2.Stop();
        }

        static TestContainer con3 = new TestContainer();
        static void OnNewLinkServer1(object p, LinkEventArgs e)
        {

            //Console.WriteLine("CL1 CON");
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

            //Console.WriteLine("CL1 CON");
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
                Console.WriteLine("Decode error");
            }
        }
    }
}
