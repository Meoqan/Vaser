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
    /// <summary>
    /// This class is used for opening connections to servers.
    /// Use: VaserClient.ConnectClient(...);
    /// </summary>
    public class VaserClient
    {

        /// <summary>
        /// Connect to a Vaser server.
        /// </summary>
        /// <param name="IP">hostname or IPAddress</param>
        /// <param name="Port">3000</param>
        /// <param name="Mode">VaserOptions modes</param>
        /// <returns>Returns null if the client can't connect</returns>
        public static Link ConnectClient(string IP, short Port, VaserOptions Mode, PortalCollection PColl)
        {
            if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IP, Port);
                if (client.Connected)
                {
                    Connection con = new Connection(client, false, Mode, PColl);

                    lock (Link._Static_ThreadLock)
                    {
                        Link.LinkList.Add(con.link);
                    }

                    return con.link;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Connect to a Vaser server.
        /// </summary>
        /// <param name="ip">hostname or IPAddress</param>
        /// <param name="port">3000</param>
        /// <param name="Mode">VaserOptions modes</param>
        /// <param name="CertCol">Certificate Collection for SSL encryption</param>
        /// <param name="TargetHostname">Hostname of the remote machine</param>
        /// <returns>Returns null if the client can't connect</returns>
        public static Link ConnectClient(string IP, short Port, VaserOptions Mode, PortalCollection PColl, X509Certificate2Collection CertCol, string TargetHostname)
        {
            if (Mode == VaserOptions.ModeSSL && CertCol == null) throw new Exception("Missing X509Certificate2 in ConnectClient(string ip, short port, VaserOptions Mode, X509Certificate Cert)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IP, Port);
                if (client.Connected)
                {
                    Connection con = new Connection(client, false, VaserOptions.ModeSSL, PColl,null, CertCol, TargetHostname);

                    lock(Link._Static_ThreadLock)
                    {
                        Link.LinkList.Add(con.link);
                    }

                    return con.link;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}
