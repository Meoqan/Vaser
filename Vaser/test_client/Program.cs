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

            //Client initalisieren
            Portal system = new Portal();

            TCPClient Client1 = new TCPClient();
            TCPClient Client2 = new TCPClient();

            Link lnk1 = Client1.ConnectClient("localhost", 3100);
            Link lnk2 = Client2.ConnectClient("localhost", 3101);

            if (lnk1 != null) Console.WriteLine("1: Verbindung erfolgreich aufgebaut.");
            if (lnk2 != null) Console.WriteLine("2: Verbindung erfolgreich aufgebaut.");

            //arbeiten
            while (online)
            {
                
                con1.test = "INFORMATION! lnk1";
                system.SendContainer(lnk1, con1, 1, 1);
                con1.test = "INFORMATION! lnk2";
                system.SendContainer(lnk2, con1, 1, 1);

                Portal.finialize();
               
                //verarbeiten
                foreach (Packet_Recv pak in system.getPakets())
                {
                    con1.UnpackDataObject(pak, system);
                    Console.WriteLine(con1.test);
                    //zustellen

                }
                Thread.Sleep(1000);
                //entfernen
            }
            Client1.CloseClient();
            Client2.CloseClient();
        }
    }
}
