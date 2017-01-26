using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Vaser;

namespace VaserServerTemplate
{
    class Program
    {
        static VaserServer Server = null;
        static PortalCollection CPoll = null;

        internal static Portal vSystem = null;

        static void Main(string[] args)
        {
            Start();
            Console.ReadKey();
            Stop();

        }
        
        public static void Start()
        {
            Debug.WriteLine("Starting Server");

            vSystem = new Portal(1);
            vSystem.IncomingPacket += OnSystemPacket;

            CPoll = new PortalCollection();
            CPoll.RegisterPortal(vSystem);

            Server = new VaserServer(System.Net.IPAddress.Any, 4500, CPoll);

            Debug.WriteLine("Starting Done");
        }

        public static void Stop()
        {
            Debug.WriteLine("Stopping Server");

            Server.Stop();

            Debug.WriteLine("Stopping Done");
        }


        static void OnNewLink(object p, LinkEventArgs e)
        {
            Debug.WriteLine("New client connected: " + e.lnk.IPv4Address);

            e.lnk.EmptyBuffer += OnEmptyBuffer;

            Client c = new Client();
            c.lnk = e.lnk;
            lock(Client.ClientList)
            {
                Client.ClientList.Add(c);
            }

            e.lnk.Accept();
        }

        static void OnDisconnectingLink(object p, LinkEventArgs e)
        {
            Debug.WriteLine("Client disconnected");
            try
            {
                Client c = (Client)e.lnk.vCDObject;

                e.lnk.EmptyBuffer -= OnEmptyBuffer;

                lock (Client.ClientList)
                {
                    Client.ClientList.Remove(c);
                }

            }
            catch
            {

            }
        }

        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            Client c = (Client)e.lnk.vCDObject;
            if (c == null)
            {
                e.lnk.Dispose();
                return;
            }

        }

        static void OnEmptyBuffer(object p, LinkEventArgs e)
        {
        }


    }
}