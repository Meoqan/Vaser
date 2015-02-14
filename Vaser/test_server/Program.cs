using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;

namespace test_server
{
    public class Program
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

            //initialize the server
            Portal system = new Portal();
            //start the server
            TCPServer Server1 = new TCPServer(System.Net.IPAddress.Any, 3100);
            TCPServer Server2 = new TCPServer(System.Net.IPAddress.Any, 3101);

            //arbeiten
            while (online)
            {
                //neuen Client annehmen
                Link lnk1 = Server1.GetNewLink();
                if (lnk1 != null)
                {
                    lnk1.Accept();

                    con1.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
                    system.SendContainer(lnk1, con1, 1, 1);
                }

                //neuen Client annehmen
                Link lnk2 = Server2.GetNewLink();
                if (lnk2 != null)
                {
                    lnk2.Accept();

                    con1.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
                    system.SendContainer(lnk2, con1, 1, 1);
                }

                //proceed incoming data
                foreach(Packet_Recv pak in system.getPakets())
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
                Portal.Finialize();
                //entfernen
            }

            //server schließen
            Server1.Stop();
            Server2.Stop();
        }
    }
}
