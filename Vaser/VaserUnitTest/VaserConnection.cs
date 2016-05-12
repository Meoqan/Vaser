using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vaser;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace VaserUnitTest
{
    
    [TestClass]
    public class VaserConnection
    {
        [TestMethod]
        [ExpectedException(typeof(SocketException))]
        public void FailConnection()
        {
            Portal system = new Portal(100);
            PortalCollection PColl = new PortalCollection();
            PColl.RegisterPortal(system);
            Link lnkC = VaserClient.ConnectClient("localhost", 3110, PColl);
        }

        AutoResetEvent autoEvent = new AutoResetEvent(false);

        [TestMethod]
        public void ShortTest()
        {

            TestServer srv = TestServer.CreateServer();
            srv.InitPortals();
            srv.InitServer();
            srv.DelegatePortals();
            srv.StartServer();
            

            TestClient client = TestClient.CreateClient();
            client.InitPortals();
            client.DelegatePortals();
            client.ConnectClient();


            srv.autoEvent.WaitOne();

            srv.StopServer();
            

        }
        
    }
}
