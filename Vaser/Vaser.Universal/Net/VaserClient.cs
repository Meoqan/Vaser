﻿using System;
using Windows.Networking.Sockets;
using Vaser.ConnectionSettings;
namespace Vaser
{
    /// <summary>
    /// This class is used for opening connections to servers.
    /// Use: VaserClient.ConnectClient(...);
    /// </summary>
    public class VaserClient
    {
        /// <summary>
        /// Opens an unencrypted connection to a server.
        /// </summary>
        /// <param name="IP">Hostname or IP-Address.</param>
        /// <param name="RemotePort">Target port of the remote server.</param>
        /// <param name="PColl">The Portal Collection.</param>
        /// <returns>Returns the link of the connection.</returns>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown if vaser is unable to create a socket or a connection.</exception>
        public static async System.Threading.Tasks.Task<Link> ConnectClient(string IP, int RemotePort, PortalCollection PColl)
        {
            //if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                //Debug.WriteLine("Connecting");
                StreamSocket client = new StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                await client.ConnectAsync(serverHost, RemotePort.ToString(), SocketProtectionLevel.PlainSocket);
                //Debug.WriteLine("Connected");
                PColl._Active = true;
                //Debug.WriteLine("Create Connection class");
                Connection con = new Connection(client, false, VaserOptions.ModeNotEncrypted, PColl, null, null, null, null);
                //Debug.WriteLine("Create Connection class DONE" );

                return con.link;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Opens an kerberos encrypted connection to a server.
        /// </summary>
        /// <param name="IP">Hostname or IP-Address.</param>
        /// <param name="RemotePort">Target port of the remote server.</param>
        /// <param name="PColl">The Portal Collection.</param>
        /// <param name="Kerberos">The Kerberos connectionsettings.</param>
        /// <returns>Returns the link of the connection.</returns>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown if vaser is unable to create a socket or a connection.</exception>
        /*public static Link ConnectClient(string IP, int RemotePort, PortalCollection PColl, VaserKerberosClient Kerberos)
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
                    await client.ConnectAsync(serverHost, RemotePort.ToString(), SocketProtectionLevel.);

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
        /// <param name="IP">Hostname or IP-Address.</param>
        /// <param name="RemotePort">Target port of the remote server.</param>
        /// <param name="PColl">The Portal Collection.</param>
        /// <param name="SSL">The SSL connectionsettings.</param>
        /// <returns>Returns the link of the connection.</returns>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown if vaser is unable to create a socket or a connection.</exception>
        public static async System.Threading.Tasks.Task<Link> ConnectClient(string IP, int RemotePort, PortalCollection PColl, VaserSSLClient SSL)
        {
            if (SSL == null) throw new Exception("Missing SSL options in ConnectClient(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                StreamSocket client = new StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName(IP);
                await client.ConnectAsync(serverHost, RemotePort.ToString(), SocketProtectionLevel.Tls12);

                PColl._Active = true;
                Connection con = new Connection(client, false, VaserOptions.ModeSSL, PColl, null, null, null, SSL);
                
                return con.link;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}
