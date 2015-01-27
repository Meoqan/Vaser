using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Vaser
{
    public class TCPClient
    {
        object threadlock = new object();

        TcpClient client = null;
        NetworkStream stream = null;
        public NetworkStream clientStream;
        Connection con;

        public Link ConnectClient(string ip, short port)
        {
            try
            {
                //toolbox.logwriter.add("CLIENT IS AWAKING", 3);
                client = new TcpClient();
                client.Connect(ip, port);
                if (client.Connected)
                {
                    //toolbox.logwriter.add("CLIENT HAS CONNECTED", 3);
                    con = new Connection(client, false);

                    Link.LinkList.Add(con.link);

                    return con.link;
                }
                else
                {
                    return null;
                    //throw new Exception("client not connected");
                    //toolbox.logwriter.add("CLIENT IS NOT CONNECTED", 3);
                }
            }
            catch (Exception e)
            {
                throw e;
                //toolbox.logwriter.add("CLIENT HAS A EXEPTION: " + e.ToString(), 3);
            }
        }

        public void CloseClient()
        {
            try
            {
                if (con != null) con.Stop();
            }
            finally
            {
                con = null;
            }

            stream = null;
            client = null;
        }
    }
}
