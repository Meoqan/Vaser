using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Vaser;

namespace test_server
{
    public class Program
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            //maximum packetsize is 65000 bytes
            public int ID = 1;
            public string test = "test text!";

            public byte[] by = new byte[1000];
        }

        static void Main(string[] args)
        {

            Console.WriteLine("Welcome to the Vaser testengine");
            Console.WriteLine("Prepareing server");
            Console.WriteLine("");

            Console.WriteLine("Creating container: TestContainer");
            // create new container
            TestContainer con1 = new TestContainer();

            //create connection managing lists
            
            Link test1 = null;


            //initialize the server
            Console.WriteLine("Creating portal: system");
            Portal system = new Portal();


            //Create a TestCert in CMD: makecert -sr LocalMachine -ss root -r -n "CN=localhost" -sky exchange -sk 123456
            // Do not use in Production | do not use localhost -> use your machinename!

            Console.WriteLine("Import Test Cert");
            //Import Test Cert
            X509Certificate2 cert = new X509Certificate2();

            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=localhost", false);
            store.Close();

            if (certificates.Count == 0)
            {
                Console.WriteLine("Server certificate not found");
                Console.ReadKey();
                return;
            }
            else
            {
                Console.WriteLine("Test Cert was found");
                cert = certificates[0];
            }

            // Get the value.
            string resultsTrue = cert.ToString(true);
            // Display the value to the console.
            Console.WriteLine(resultsTrue);
            // Get the value.
            string resultsFalse = cert.ToString(false);
            // Display the value to the console.
            Console.WriteLine(resultsFalse);

            //start the server
            Console.WriteLine("Creating server: IPAddress any, Port 3100, VaserMode ModeSSL");

            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, VaserOptions.ModeSSL, cert);


            Stopwatch watch = new Stopwatch();

            Console.WriteLine("");
            int testmode = 0;

            bool online = true;
            //working
            while (online)
            {
                switch (testmode)
                {
                    case 0:
                        Console.WriteLine("Test 1: Connection and Transfer statistics:");
                        Console.WriteLine("Waiting for Connection...");
                        testmode = 1;
                        break;
                    case 1:


                        Link lnk1 = Server1.GetNewLink();
                        if (lnk1 != null)
                        {
                            test1 = lnk1;
                            Console.WriteLine("New client connected: remote IPAdress {0} , remote Identity: {1}", lnk1.IPv4Address.ToString(), lnk1.UserName);
                            lnk1.Accept();

                            Console.WriteLine("Reading metadata from Link:");
                            Console.WriteLine("lnk1.Connect.StreamIsConnected is {0}", lnk1.Connect.StreamIsConnected.ToString());
                            Console.WriteLine("lnk1.Connect.ThreadIsRunning is {0}", lnk1.Connect.ThreadIsRunning.ToString());
                            Console.WriteLine("lnk1.Connect.IPv4Address is {0}", lnk1.Connect.IPv4Address.ToString());

                            testmode = 2;
                            Console.WriteLine("Send 10000 Packets....");
                            con1.test = "Message ";
                            for (int x = 0; x < 10000; x++)
                            {
                                con1.ID++;
                                system.SendContainer(test1, con1, 1, 1);
                                Portal.Finialize();
                            }
                            
                        }

                        break;
                    case 2:

                        

                        foreach (Packet_Recv pak in system.GetPakets())
                        {
                            // [1] now you can sort the packet to the right container and object
                            //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                            //unpack the packet, true if the decode was successful
                            if (con1.UnpackContainer(pak, system))
                            {
                                if (watch.IsRunning == false) watch.Start();

                                if (con1.ID == 1000) Console.WriteLine("Recived " + con1.ID);
                                if (con1.ID == 2500) Console.WriteLine("Recived " + con1.ID);
                                if (con1.ID == 5000) Console.WriteLine("Recived " + con1.ID);
                                if (con1.ID == 7500) Console.WriteLine("Recived " + con1.ID);
                                if (con1.ID == 10000)
                                {
                                    watch.Stop();
                                    Console.WriteLine("Recived " + con1.ID);

                                    Console.WriteLine("Time taken: {0} Milliseconds", watch.ElapsedMilliseconds.ToString());
                                    Console.WriteLine("Transferred: {0} Mbytes", (con1.by.Length * 100000.0) / 1024.0 / 1024.0);
                                    Console.WriteLine("Mirror Transfer rate: {0} Mbytes/second", (1000.0 / (int)watch.ElapsedMilliseconds) * ((con1.by.Length * 100000.0) / 1024.0 / 1024.0));
                                    Console.WriteLine("start ping test...");
                                    
                                    
                                    
                                    con1.ID = 0;
                                    watch.Reset();

                                    testmode = 3;
                                    system.SendContainer(test1, con1, 1, 1);
                                    Portal.Finialize();
                                    watch.Start();
                                    
                                }
                            }
                        }

                        break;

                    case 3:

                        foreach (Packet_Recv pak in system.GetPakets())
                        {
                            watch.Stop();

                            Console.WriteLine("responsetime is: {0} Milliseconds", watch.ElapsedMilliseconds.ToString());

                            Console.WriteLine("Try to close the connection...");
                            testmode = 4;
                        }

                        
                        break;
                    case 4:
                        test1.Dispose();
                        Console.WriteLine("Closed");
                        testmode = -1;
                        break;
                }


                if (testmode == -1) break;
                //Time tick
                Thread.Sleep(1);
            }

            //close Server
            Console.WriteLine("Close Server");
            Server1.Stop();
            


            Console.WriteLine("Test ended. Press any key...");
            Console.ReadKey();
        }
    }
}
