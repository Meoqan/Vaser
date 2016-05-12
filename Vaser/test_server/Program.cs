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

        // create new container

        static int testmode = 0;
        static Stopwatch watch = new Stopwatch();
        static Link test1 = null;
        static Portal system = null;
        static void Main(string[] args)
        {

            Console.WriteLine("Welcome to the Vaser testengine");
            Console.WriteLine("Prepareing server");
            Console.WriteLine("");

            Console.WriteLine("Creating container: TestContainer");


            //create connection managing lists



            //initialize the server
            Console.WriteLine("Creating portal: system");
            system = new Portal(100);
            PortalCollection PC = new PortalCollection();
            PC.RegisterPortal(system);

            system.IncomingPacket += OnSystemPacket;

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
            VaserSSLServer ssl = new VaserSSLServer(certificates[0]);
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3100, PC, ssl);
            Server1.NewLink += OnNewLink;

            Server1.Start();

            Console.WriteLine("");


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

        static object Buffer_lock = new object();

        static TestContainer con2 = new TestContainer();
        static void OnNewLink(object p, LinkEventArgs e)
        {

            test1 = e.lnk;
            Console.WriteLine("New client connected: remote IPAdress {0} , remote Identity: {1}", e.lnk.IPv4Address.ToString(), e.lnk.UserName);

            e.lnk.EmptyBuffer += OnEmptyBuffer;
            e.lnk.Accept();
            
            Console.WriteLine("Reading metadata from Link:");
            Console.WriteLine("lnk1.Connect.StreamIsConnected is {0}", e.lnk.IsConnected.ToString());
            Console.WriteLine("lnk1.Connect.IPv4Address is {0}", e.lnk.IPv4Address.ToString());

            testmode = 2;
            Console.WriteLine("Send 1000 Packets....");
            con2.test = "Message ";

            try {
                lock (Buffer_lock)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        con2.ID++;
                        system.SendContainer(test1, con2, 1, 1, true);
                    }
                    //Portal.Finialize();
                }
            }catch(Exception es)
            {
                Console.WriteLine(es.ToString());
            }
        }

        static TestContainer con3 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            //Debug.WriteLine("Event called!");
            switch (testmode)
            {
                case 2:

                    //unpack the packet, true if the decode was successful
                    if (con3.UnpackContainer(e.pak, e.portal))
                    {
                        if (watch.IsRunning == false) watch.Start();

                        if (con3.ID == 100) Console.WriteLine("Recived " + con3.ID);
                        if (con3.ID == 250) Console.WriteLine("Recived " + con3.ID);
                        if (con3.ID == 500) Console.WriteLine("Recived " + con3.ID);
                        if (con3.ID == 750) Console.WriteLine("Recived " + con3.ID);
                        if (con3.ID == 1000)
                        {
                            watch.Stop();
                            Console.WriteLine("Recived " + con3.ID);

                            Console.WriteLine("Time taken: {0} Milliseconds", watch.ElapsedMilliseconds.ToString());
                            Console.WriteLine("Transferred: {0} Mbytes", (con3.by.Length * 1000.0) / 1024.0 / 1024.0);
                            Console.WriteLine("Mirror Transfer rate: {0} Mbytes/second", (1000.0 / (int)watch.ElapsedMilliseconds) * ((con3.by.Length * 1000.0) / 1024.0 / 1024.0));
                            Console.WriteLine("start ping test...");



                            con3.ID = 0;
                            watch.Reset();

                            testmode = 3;
                            e.portal.SendContainer(test1, con3, 1, 1);
                            //Portal.Finialize();
                            watch.Start();


                        }
                    }
                    break;
                case 3:

                    watch.Stop();

                    Console.WriteLine("responsetime is: {0} Milliseconds", watch.ElapsedMilliseconds.ToString());

                    Console.WriteLine("Try to close the connection...");
                    testmode = 4;



                    break;
            }
        }

        static void OnEmptyBuffer(object p, LinkEventArgs e)
        {
            //Console.WriteLine("OnEmptyBuffer called!");
            lock (Buffer_lock)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (con2.ID < 1000)
                    {
                        con2.ID++;
                        system.SendContainer(test1, con2, 1, 1, true);
                    }
                }
                //Portal.Finialize();
            }
        }
    }
}

