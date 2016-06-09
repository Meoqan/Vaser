﻿using System;
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


        static object Livinglist_lock = new object();
        static List<Link> Livinglist = new List<Link>();

        static void Main(string[] args)
        {
           
            bool online = true;

            //Client initalisieren
            Portal system = new Portal(100);
            PortalCollection PC = new PortalCollection();
            PC.RegisterPortal(system);

            system.IncomingPacket += OnSystemPacket;


            Thread.Sleep(1000);

            //while (online)
            //{
            int counter = 0;
            while (counter < 1000)
            {
                counter++;
                Link lnk1 = VaserClient.ConnectClient("localhost", 3100, PC);
                lnk1.Disconnecting += OnDisconnectingLink;
                counter++;
                Link lnk2 = VaserClient.ConnectClient("localhost", 3101, PC);
                lnk2.Disconnecting += OnDisconnectingLink;

                if (lnk1 != null)
                {
                    //Console.WriteLine("1: successfully established connection.");
                    lock (Livinglist_lock)
                    {
                        Livinglist.Add(lnk1);
                    }
                }
                /*if (lnk2 != null)
                {
                    //Console.WriteLine("2: successfully established connection.");
                    lock (Livinglist_lock)
                    {
                        Livinglist.Add(lnk2);
                    }
                }*/
            }

            //Thread.Sleep(1);
            Console.ReadKey();
            //}
        }

        static void OnDisconnectingLink(object p, LinkEventArgs e)
        {
            //Console.WriteLine("CL1 CON");
            lock (Livinglist_lock)
            {
                Livinglist.Remove(e.lnk);
            }
        }

        static uint counter = 0;
        static TestContainer con2 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            switch (e.pak.ContainerID)
            {
                case 1:

                    // [1] now you can sort the packet to the right container and object
                    //Console.WriteLine("the packet has the container ID {0} and is for the object ID {1} ", pak.ContainerID, pak.ObjectID);

                    //unpack the packet, true if the decode was successful
                    if (con2.UnpackContainer(e.pak, e.portal))
                    {
                        if (con2.ID < 0) Console.WriteLine("Decode error: " + con2.ID);
                        //if (con2.ID > 100) Console.WriteLine("Decode error: " + con2.ID);
                        if (con2.ID < 500)
                        {
                            //Console.WriteLine("Ping! " + counter + " CounterID" + con2.ID + " Object:" + pak.ObjectID);

                            con2.ID += 1;
                            e.portal.SendContainer(e.pak.link, con2, 1, e.pak.ObjectID);
                        }
                        else
                        {
                            counter++;
                            Console.WriteLine("Disconnecting! " + counter + " CounterID" + con2.ID + " Object:" + e.pak.ObjectID);
                            e.pak.link.Dispose();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Decode error");
                    }
                    break;
            }
        }
    }
}
