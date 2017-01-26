using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using Vaser.ConnectionSettings;

namespace Vaser
{
    /// <summary>
    /// This class is used to start servers.
    /// Use: VaserServer srv = new VaserServer(...);
    /// </summary>
    public class VaserServer
    {
        //private object _ThreadLock = new object();
        private TcpListener _TCPListener;
        //private Thread _ListenThread;
        private volatile bool _ServerOnline = true;
        private Timer _aTimer;
        private static Timer _GCTimer;

        private object _ConnectionList_ThreadLock = new object();
        private List<Connection> _ConnectionList = new List<Connection>();

        private object _NewLinkList_ThreadLock = new object();
        private List<Link> _NewLinkList = new List<Link>();

        private VaserOptions _ServerOption = null;
        private VaserKerberosServer _vKerberos = null;
        private VaserSSLServer _vSSL = null;

        private PortalCollection _PCollection = null;

        /// <summary>
        /// EventHandler for new connected links.
        /// </summary>
        public event EventHandler<LinkEventArgs> NewLink;

        /// <summary>
        /// EventHandler for disconnecting links.
        /// </summary>
        public event EventHandler<LinkEventArgs> DisconnectingLink;


        public PortalCollection PCollection
        {
            get
            {
                return _PCollection;
            }
            set
            {
                _PCollection = value;
            }
        }

        public VaserOptions ServerOption
        {
            get
            {
                return _ServerOption;
            }
            set
            {
                _ServerOption = value;
            }
        }

        internal List<Connection> ConnectionList
        {
            get
            {
                return _ConnectionList;
            }
            set
            {
                _ConnectionList = value;
            }
        }


        private bool ServerOnline
        {
            get
            {
                return _ServerOnline;
            }
            set
            {
                _ServerOnline = value;
                if (!_ServerOnline)
                {
                    _aTimer.Dispose();

                }
            }
        }

        /// <summary>
        /// Stops the Vaser Server
        /// </summary>
        public void Stop()
        {
            ServerOnline = false;
            if (_TCPListener != null)
            {
                _TCPListener.Stop();
            }
            if (_aTimer != null)_aTimer.Dispose();
        }

        /// <summary>
        /// Stops Vaser
        /// </summary>
        public static void StopEngine()
        {
            Options.Operating = false;
            _GCTimer.Dispose();
        }


        /// <summary>
        /// Starts listening for clients on selected Mode.
        /// </summary>
        public void Start()
        {
            try
            {
                _TCPListener.Start();

                
                //_aTimer = new Timer(new TimerCallback(ListenForClients), null,0,5);
                

                if (_GCTimer == null)
                {
                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;
                    _GCTimer = new Timer(new TimerCallback(GC_Collect), null, 0, 15000);
                }

                new Thread(new ThreadStart(ListenForClients)).Start();
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new unencrypted TCP Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        /// <param name="PortalCollection">the Portal Collection</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl)
        {
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                ServerOption = VaserOptions.ModeNotEncrypted;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new Kerberos Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        /// <param name="PortalCollection">the Portal Collection</param>
        /// <param name="Kerberos">Kerberos connection settings</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl, VaserKerberosServer Kerberos)
        {
            if (Kerberos == null) throw new Exception("Missing Kerberos options in VaserServer(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {

                _vKerberos = Kerberos;
                ServerOption = VaserOptions.ModeKerberos;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new SSL Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        /// <param name="PortalCollection">the Portal Collection</param>
        /// <param name="SSL">SSL connection settings</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl, VaserSSLServer SSL)
        {
            if (SSL == null) throw new Exception("Missing SSL options in VaserServer(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");
            try
            {
                _vSSL = SSL;
                ServerOption = VaserOptions.ModeSSL;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GC_Collect(Object source)
        {
            GC.Collect();
        }

        private async void ListenForClients()
        {
            try
            {
                while(ServerOnline && Options.Operating)
                {

                    Socket Client = await _TCPListener.AcceptSocketAsync();
                    //ThreadPool.QueueUserWorkItem(QueueNewConnection, Client);
                    QueueNewConnection(Client);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR in VaserServer.ListenForClients() > " + ex.ToString());
            }

            if (!ServerOnline || !Options.Operating)
            {
                _aTimer.Dispose();

                lock (_ConnectionList_ThreadLock)
                {
                    foreach (Connection Con in _ConnectionList)
                    {
                        Con.Stop();
                    }
                    _ConnectionList.Clear();
                }

                _TCPListener.Stop();
            }
        }

        internal void QueueNewConnection(object Client)
        {
            try
            {
                Connection con = new Connection((Socket)Client, true, _ServerOption, _PCollection, _vKerberos, _vSSL, null, null, this);

                lock (_ConnectionList_ThreadLock)
                {
                    _ConnectionList.Add(con);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR in VaserServer.QueueNewConnection(object Client) > " + e.ToString());
            }
        }

        internal void AddNewLink(Link lnk)
        {
            lock (_NewLinkList_ThreadLock)
            {
                _NewLinkList.Add(lnk);

                if (!NewQueueLock)
                {
                    NewQueueLock = true;
                    ThreadPool.QueueUserWorkItem(NewEventWorker);
                }
            }


        }
        volatile bool NewQueueLock = false;
        private void NewEventWorker(object threadContext)
        {
            List<Link> LinkListTEMP = GetNewLinkList();
            while (LinkListTEMP.Count != 0)
            {
                foreach (Link lnk in LinkListTEMP)
                {
                    LinkEventArgs args = new LinkEventArgs();
                    args.lnk = lnk;

                    OnNewLink(args);
                }
                LinkListTEMP = GetNewLinkList();
            }

        }

        private List<Link> GetNewLinkList()
        {
            List<Link> LinkListTEMP = null;

            lock (_NewLinkList_ThreadLock)
            {

                LinkListTEMP = _NewLinkList;
                _NewLinkList = new List<Link>();
                if (LinkListTEMP.Count == 0) NewQueueLock = false;
            }

            return LinkListTEMP;
        }

        protected virtual void OnNewLink(LinkEventArgs e)
        {

            NewLink?.Invoke(this, e);
        }

        internal void RemoveFromConnectionList(Connection con)
        {
            if (con == null) return;

            lock (_ConnectionList_ThreadLock)
            {
                _ConnectionList.Remove(con);
                LinkEventArgs args = new LinkEventArgs();
                args.lnk = con.link;

                OnDisconnectingLink(args);
            }

        }


        protected virtual void OnDisconnectingLink(LinkEventArgs e)
        {

            DisconnectingLink?.Invoke(this, e);
        }

    }

    public class LinkEventArgs : EventArgs
    {
        public Link lnk { get; set; }
    }
}
