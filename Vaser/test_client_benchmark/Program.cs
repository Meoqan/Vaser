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
            public int[] array = new int[1000];
        }

        static void Main(string[] args)
        {
            // create new container
            TestContainer con1 = new TestContainer();
            TestContainer con2 = new TestContainer();

            bool online = true;

            //Client initalisieren
            Portal system = new Portal();

            

            

            //arbeiten
            while (online)
            {

                Link lnk1 = VaserClient.ConnectClient("localhost", 3100, VaserOptions.ModeKerberos);
                Link lnk2 = VaserClient.ConnectClient("localhost", 3101, VaserOptions.ModeKerberos);

                if (lnk1 != null) Console.WriteLine("1: successfully established connection.");
                if (lnk2 != null) Console.WriteLine("2: successfully established connection.");


                //send data
                con1.test = "Data Send.";
                // the last 2 digits are manually set [1]
                system.SendContainer(lnk1, con1, 1, 1);
                con1.test = "Data Send.";
                system.SendContainer(lnk2, con1, 1, 1);

                Portal.Finialize();

                int wait = 2;
                while (wait == 2)
                {
                    //proceed incoming data
                    foreach (Packet_Recv pak in system.getPakets())
                    {
                        // [1] now you can sort the packet to the right container and object
                        Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                        //unpack the packet, true if the decode was successful
                        if (con1.UnpackDataObject(pak, system))
                        {
                            Console.WriteLine(con1.test);
                            if (con1.test == "Data Send.") wait=1;
                        }
                    }
                    
                }
                Thread.Sleep(1000);
                //remove
                lnk1.Dispose();
                lnk2.Dispose();

            }
        }
    }
}
