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
            public int[] array = new int[1000];
        }

        static void Main(string[] args)
        {
            // create new container
            TestContainer con1 = new TestContainer();
            TestContainer con2 = new TestContainer();

            bool online = true;

            //initialize the server
            Portal system = new Portal();
            //start the server
            TCPServer Server1 = new TCPServer(System.Net.IPAddress.Any, 3100);
            TCPServer Server2 = new TCPServer(System.Net.IPAddress.Any, 3101);

            //create connection managing lists
            List<Link> Livinglist = new List<Link>();
            List<Link> Removelist = new List<Link>();

            //run the server
            while (online)
            {
                //accept new client
                Link lnk1 = Server1.GetNewLink();
                if (lnk1 != null)
                {
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
                    Livinglist.Add(lnk2);
                    lnk2.Accept();

                    //send data
                    con1.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
                    // the last 2 digits are manually set [1]
                    system.SendContainer(lnk2, con1, 1, 1);
                }

                //proceed incoming data
                foreach (Packet_Recv pak in system.getPakets())
                {
                    // [1] now you can sort the packet to the right container and object
                    Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con1.UnpackDataObject(pak, system))
                    {
                        Console.WriteLine(con1.test);

                        // the last 2 digits are manually set [1]
                        system.SendContainer(pak.link, con1, 1, 1);
                    }
                }

                //send all bufferd data to the clients
                Portal.Finialize();

                Thread.Sleep(10);

                //disconnet clients
                foreach (Link l in Livinglist)
                {
                    con2.test = "beep.";
                    con2.array = new int[1];
                    system.SendContainer(l, con2, 1, 1);

                    if (!l.Connect.StreamIsConnected) Removelist.Add(l);
                }

                foreach(Link l in Removelist)
                {
                    Livinglist.Remove(l);
                    //free all resources
                    l.Dispose();
                }
                Removelist.Clear();

                Thread.Sleep(10);
            }

            //close the server
            Server1.Stop();
            Server2.Stop();
        }
    }
}
