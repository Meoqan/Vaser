using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Vaser.global;
using System.Security.Cryptography.X509Certificates;

namespace Vaser
{
    public class VaserServer
    {
        private SemaphoreSlim _ThreadLock = new SemaphoreSlim(1);
        private TcpListener _TCPListener;
        private Thread _ListenThread;
        private bool _ServerOnline = true;

        private SemaphoreSlim _ConnectionList_ThreadLock = new SemaphoreSlim(1);
        private List<Connection> _ConnectionList = new List<Connection>();
        private List<Link> NewLinkList = new List<Link>();

        private X509Certificate2 _Certificate = null;
        private VaserOptions _ServerOption = null;

        public X509Certificate2 Certificate
        {
            get
            {
                _ThreadLock.Wait();
                X509Certificate2 ret = _Certificate;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _Certificate = value;
                _ThreadLock.Release();
            }
        }

        public VaserOptions ServerOption
        {
            get
            {
                _ThreadLock.Wait();
                VaserOptions ret = _ServerOption;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _ServerOption = value;
                _ThreadLock.Release();
            }
        }

        public List<Connection> ConnectionList
        {
            get
            {
                _ThreadLock.Wait();
                List<Connection> ret = _ConnectionList;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _ConnectionList = value;
                _ThreadLock.Release();
            }
        }


        private bool ServerOnline
        {
            get
            {
                _ThreadLock.Wait();
                bool ret = _ServerOnline;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _ServerOnline = value;
                _ThreadLock.Release();
            }
        }

        /// <summary>
        /// Stops the TCP Server
        /// </summary>
        public void Stop()
        {
            _ThreadLock.Wait();
            _ServerOnline = false;
            _ThreadLock.Release();
        }

        /// <summary>
        /// Stops Vaser
        /// </summary>
        public void StopEngine()
        {
            Options.Operating = false;
        }

        /// <summary>
        /// Creates a new TCP Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        /// <param name="Mode">VaserOptions.ModeKerberos</param>
        public VaserServer(IPAddress LocalAddress, int Port, VaserOptions Mode)
        {
            if (Mode == VaserOptions.ModeSSL) throw new Exception("Missing X509Certificate2");

            _ThreadLock.Wait();
            _ServerOption = Mode;
            _TCPListener = new TcpListener(LocalAddress, Port);
            _ListenThread = new Thread(new ThreadStart(ListenForClients));
            _ListenThread.Start();
            _ThreadLock.Release();
        }

        /// <summary>
        /// Creates a new TCP Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        /// <param name="Mode">VaserOptions.ModeSSL</param>
        /// <param name="Cert">X509Certificate</param>
        public VaserServer(IPAddress LocalAddress, int Port, VaserOptions Mode, X509Certificate2 Cert)
        {
            if (Mode == VaserOptions.ModeSSL && Cert == null) throw new Exception("Missing X509Certificate2 in VaserServer(IPAddress LocalAddress, int Port, VaserOptions Mode, X509Certificate Cert)");

            _ThreadLock.Wait();
            _Certificate = Cert;
            _ServerOption = Mode;
            _TCPListener = new TcpListener(LocalAddress, Port);
            _ListenThread = new Thread(new ThreadStart(ListenForClients));
            _ListenThread.Start();
            _ThreadLock.Release();
        }


        private void ListenForClients()
        {
            _TCPListener.Start();

            while (ServerOnline && Options.Operating)
            {
                while (_TCPListener.Pending())
                {
                    
                    TcpClient Client = this._TCPListener.AcceptTcpClient();

                    ThreadPool.QueueUserWorkItem(QueueNewConnection, Client);

                }

                Thread.Sleep(100);
            }

            _ConnectionList_ThreadLock.Wait();
            foreach (Connection Con in _ConnectionList)
            {
                Con.Stop();
            }
            _ConnectionList_ThreadLock.Release();

            _TCPListener.Stop();
        }

        internal void QueueNewConnection(object Client)
        {
            Connection con = new Connection((TcpClient)Client, true, _ServerOption, _Certificate, this);

            _ConnectionList_ThreadLock.Wait();
            _ConnectionList.Add(con);
            _ConnectionList_ThreadLock.Release();
        }

        internal void RemoveFromConnectionList(Connection con)
        {
            _ConnectionList_ThreadLock.Wait();
            _ConnectionList.Remove(con);
            _ConnectionList_ThreadLock.Release();
        }

        /// <summary>
        /// Get a new Connected Client.
        /// </summary>
        /// <returns>Returns null if no new client is connected</returns>
        public Link GetNewLink()
        {
            _ThreadLock.Wait();
            Link lnk = null;

            if (NewLinkList.Count > 0)
            {
                lnk = NewLinkList[0];
                NewLinkList.Remove(lnk);
            }
            _ThreadLock.Release();

            return lnk;
        }

        internal void AddNewLink(Link lnk)
        {
            _ThreadLock.Wait();
            NewLinkList.Add(lnk);
            _ThreadLock.Release();
        }
    }
}
