using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vaser;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;

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
            Link lnkC = VaserClient.ConnectClient("localhost", 3100, VaserOptions.ModeKerberos);
        }

        [TestMethod]
        public void TestServer()
        {
            // create new container
            TestContainer con1 = new TestContainer();

            bool online = true;

            //initialize the server
            Portal system = new Portal();
            //start the server
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, VaserOptions.ModeKerberos);

            Link lnkC = VaserClient.ConnectClient("localhost", 3100, VaserOptions.ModeKerberos);
            

            if (lnkC != null) Console.WriteLine("1: successfully established connection.");


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
                
                //proceed incoming data
                foreach (Packet_Recv pak in system.GetPakets())
                {
                    // [1] now you can sort the packet to the right container and object
                    Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    if (pak.ObjectID == 1)
                    {
                        //unpack the packet, true if the decode was successful
                        if (con1.UnpackContainer(pak, system))
                        {
                            Console.WriteLine(con1.test);

                            // the last 2 digits are manually set [1]
                            system.SendContainer(pak.link, con1, 1, 2);
                        }
                    }
                    else
                    {
                        lnkC.Dispose();
                    }
                }

                //send all bufferd data to the clients
                Portal.Finialize();

                Thread.Sleep(10);

                //disconnet clients
                foreach (Link l in Livinglist)
                {
                    con1.test = "beep.";
                    con1.array = new int[1];
                    //system.SendContainer(l, con2, 1, 1);
                    //Console.WriteLine("beep.");
                    if (!l.Connect.StreamIsConnected) Removelist.Add(l);
                }

                foreach (Link l in Removelist)
                {
                    Livinglist.Remove(l);
                    //free all resources
                    l.Dispose();
                    Console.WriteLine("Client disconnected");
                    online = false;

                }
                Removelist.Clear();

                Thread.Sleep(1);
            }

            //close the server
            Server1.Stop();

            
        }
    }
}
