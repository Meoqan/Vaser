﻿using System;
using Windows.Networking.Sockets;
using Vaser.ConnectionSettings;
using System.Diagnostics;
namespace Vaser
{
    /// <summary>
    /// This class is used for opening connections to servers.
    /// Use: VaserClient.ConnectClient(...);
    /// </summary>
    public class VaserClient
    {
        /// <summary>
        /// Connect to a Vaser server unencrypted.
        /// </summary>
        /// <param name="IP">hostname or IPAddress</param>
        /// <param name="Port">3000</param>
        /// <param name="PortalCollection">the Portal Collection</param>
        /// <returns>Returns the link to the client</returns>
        public static async System.Threading.Tasks.Task<Link> ConnectClient(string IP, int Port, PortalCollection PColl)
        {
            //if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                //Debug.WriteLine("Connecting");
                StreamSocket client = new StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                await client.ConnectAsync(serverHost, Port.ToString(), SocketProtectionLevel.PlainSocket);
                //Debug.WriteLine("Connected");
                PColl._Active = true;
                //Debug.WriteLine("Create Connection class");
                Connection con = new Connection(client, false, VaserOptions.ModeNotEncrypted, PColl, null, null, null, null);
                //Debug.WriteLine("Create Connection class DONE" );
                lock (Link._Static_ThreadLock)
                {
                    Link.LinkList.Add(con.link);
                }

                return con.link;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Connect to a Vaser server via kerberos.
        /// </summary>
        /// <param name="IP">hostname or IPAddress</param>
        /// <param name="Port">3000</param>
        /// <param name="PortalCollection">the Portal Collection</param>
        /// <param name="Kerberos">the kerberos connection settings</param>
        /// <returns>Returns the link to the client</returns>
        /*public static Link ConnectClient(string IP, int Port, PortalCollection PColl, VaserKerberosClient Kerberos)
        {
            //if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                //TcpClient client = new TcpClient();
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                StreamSocket s = new StreamSocket();
                client.Connect(IP, Port);
                if (client.Connected)
                {
                    StreamSocket client = new StreamSocket();
                    Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                    await client.ConnectAsync(serverHost, Port.ToString(), SocketProtectionLevel.);

                    PColl._Active = true;
                    Connection con = new Connection(client, false, VaserOptions.ModeKerberos, PColl, null, null, Kerberos, null);

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
        }*/

        /// <summary>
        /// Connect to a Vaser server via SSL.
        /// </summary>
        /// <param name="ip">hostname or IPAddress</param>
        /// <param name="port">3000</param>
        /// <param name="PortalCollection">the PortalCollection</param>
        /// <param name="SSL">SSL connection settings</param>
        /// <returns>Returns the link to the client</returns>
        public static async System.Threading.Tasks.Task<Link> ConnectClient(string IP, int Port, PortalCollection PColl, VaserSSLClient SSL)
        {
            if (SSL == null) throw new Exception("Missing SSL options in ConnectClient(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                StreamSocket client = new StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                await client.ConnectAsync(serverHost, Port.ToString(), SocketProtectionLevel.Tls12);

                PColl._Active = true;
                Connection con = new Connection(client, false, VaserOptions.ModeSSL, PColl, null, null, null, SSL);

                lock (Link._Static_ThreadLock)
                {
                    Link.LinkList.Add(con.link);
                }

                return con.link;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}