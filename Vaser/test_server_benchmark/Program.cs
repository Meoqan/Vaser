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
        public class TestContainer : Container
        {
            public int ID = 1;
            public string test = "test text!";

            public int[] array = new int[1000];
        }

        static void Main(string[] args)
        {

            TestContainer con1 = new TestContainer();
            TestContainer con2 = new TestContainer();

            bool online = true;

            //Server initalisieren
            Portal system = new Portal();
            //Server starten
            TCPServer Server1 = new TCPServer(System.Net.IPAddress.Any, 3100);
            TCPServer Server2 = new TCPServer(System.Net.IPAddress.Any, 3101);

            List<Link> Livinglist = new List<Link>();
            List<Link> Removelist = new List<Link>();

            //arbeiten
            while (online)
            {
                //neuen Client annehmen
                Link lnk1 = Server1.GetNewLink();
                if (lnk1 != null)
                {
                    Livinglist.Add(lnk1);

                    lnk1.Accept();

                    con1.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
                    system.SendContainer(lnk1, con1, 1, 1);
                }

                //neuen Client annehmen
                Link lnk2 = Server2.GetNewLink();
                if (lnk2 != null)
                {
                    Livinglist.Add(lnk2);
                    lnk2.Accept();

                    con1.test = "You are connected to Server 2 via Vaser. Please send your Logindata.";
                    system.SendContainer(lnk2, con1, 1, 1);
                }

                //verarbeiten
                foreach (Packet_Recv pak in system.getPakets())
                {
                    con1.UnpackDataObject(pak, system);
                    Console.WriteLine(con1.test);
                    system.SendContainer(pak.link, con1, 1, 1);
                    //zustellen
                }
                Portal.finialize();
                //entfernen

                Thread.Sleep(10);

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
                    l.Dispose();
                }
                Removelist.Clear();

                Thread.Sleep(10);
            }

            //server schließen
            Server1.Stop();
            Server2.Stop();
        }
    }
}
