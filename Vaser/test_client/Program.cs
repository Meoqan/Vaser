using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using System.Threading;

namespace test_client
{
    class Program
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";
        }

        static void Main(string[] args)
        {
            // create new container
            TestContainer con1 = new TestContainer();
            TestContainer con2 = new TestContainer();

            bool online = true;

            //Client initalisieren
            Portal system = new Portal();

            TCPClient Client1 = new TCPClient();
            TCPClient Client2 = new TCPClient();

            Link lnk1 = Client1.ConnectClient("localhost", 3100);
            Link lnk2 = Client2.ConnectClient("localhost", 3101);

            if (lnk1 != null) Console.WriteLine("1: successfully established connection.");
            if (lnk2 != null) Console.WriteLine("2: successfully established connection.");

            //arbeiten
            while (online)
            {
                //send data
                con1.test = "INFORMATION! lnk1";
                // the last 2 digits are manually set [1]
                system.SendContainer(lnk1, con1, 1, 1);
                con1.test = "INFORMATION! lnk2";
                system.SendContainer(lnk2, con1, 1, 1);

                Portal.Finialize();

                //proceed incoming data
                foreach (Packet_Recv pak in system.getPakets())
                {
                    // [1] now you can sort the packet to the right container and object
                    Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con1.UnpackDataObject(pak, system))
                    {
                        Console.WriteLine(con1.test);
                    }
                }
                Thread.Sleep(1000);
                //entfernen
            }
            Client1.CloseClient();
            Client2.CloseClient();
        }
    }
}
