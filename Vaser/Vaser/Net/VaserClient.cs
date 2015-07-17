using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace Vaser
{
    public class VaserClient
    {
        //TcpClient client = null;
        //NetworkStream stream = null;
        //public NetworkStream clientStream;
        //Connection con;

        /// <summary>
        /// Connect to a Vaser server.
        /// </summary>
        /// <param name="ip">hostname or IPAddress</param>
        /// <param name="port">3000</param>
        /// <returns>Returns null if the client can't connect</returns>
        public static Link ConnectClient(string ip, short port, VaserOptions Mode)
        {
            if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");

            try
            {
                //toolbox.logwriter.add("CLIENT IS AWAKING", 3);
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                if (client.Connected)
                {
                    //toolbox.logwriter.add("CLIENT HAS CONNECTED", 3);
                    Connection con = new Connection(client, false, VaserOptions.ModeKerberos);
                    //Thread.Sleep(500);

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

        /// <summary>
        /// Connect to a Vaser server.
        /// </summary>
        /// <param name="ip">hostname or IPAddress</param>
        /// <param name="port">3000</param>
        /// <returns>Returns null if the client can't connect</returns>
        public static Link ConnectClient(string ip, short port, VaserOptions Mode, X509Certificate2 Cert)
        {
            if (Mode == VaserOptions.ModeSSL && Cert == null) throw new Exception("Missing X509Certificate2 in ConnectClient(string ip, short port, VaserOptions Mode, X509Certificate Cert)");

            try
            {
                //toolbox.logwriter.add("CLIENT IS AWAKING", 3);
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                if (client.Connected)
                {
                    //toolbox.logwriter.add("CLIENT HAS CONNECTED", 3);
                    Connection con = new Connection(client, false, VaserOptions.ModeSSL, Cert);
                    //Thread.Sleep(500);

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

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /*public void CloseClient()
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
        }*/
    }
}
