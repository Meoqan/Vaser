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
        //Container bauen
        public class TestContainer : Container
        {
            public int ID = 1;
            public string test = "test text!";
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

                //verarbeiten
                foreach(Packet_Recv pak in system.getPakets())
                {
                    con1.UnpackDataObject(pak, system);
                    Console.WriteLine(con1.test);
                    system.SendContainer(pak.link, con1, 1, 1);
                    //zustellen
                }
                Portal.finialize();
                //entfernen
            }

            //server schließen
            Server1.Stop();
            Server2.Stop();
        }
    }
}
