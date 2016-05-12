using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace Vaser
{
    /// <summary>
    /// This class is used to start servers.
    /// Use: VaserServer srv = new VaserServer(...);
    /// </summary>
    public class VaserServer
    {
        private object _ThreadLock = new object();
        private TcpListener _TCPListener;
        //private Thread _ListenThread;
        private bool _ServerOnline = true;
        private System.Timers.Timer _aTimer;
        private static System.Timers.Timer _GCTimer;

        private object _ConnectionList_ThreadLock = new object();
        private List<Connection> _ConnectionList = new List<Connection>();

        private object _NewLinkList_ThreadLock = new object();
        private List<Link> _NewLinkList = new List<Link>();

        private object _DisconnectingLinkList_ThreadLock = new object();
        private List<Link> _DisconnectingLinkList = new List<Link>();

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
                lock (_ThreadLock)
                {
                    return _PCollection;
                }
            }
            set
            {
                lock (_ThreadLock)
                {
                    _PCollection = value;
                }
            }
        }
        
        public VaserOptions ServerOption
        {
            get
            {
                lock (_ThreadLock)
                {
                    return _ServerOption;
                }
            }
            set
            {
                lock (_ThreadLock)
                {
                    _ServerOption = value;
                }
            }
        }

        internal List<Connection> ConnectionList
        {
            get
            {
                lock (_ThreadLock)
                {
                    return _ConnectionList;
                }
            }
            set
            {
                lock (_ThreadLock)
                {
                    _ConnectionList = value;
                }
            }
        }


        private bool ServerOnline
        {
            get
            {
                lock (_ThreadLock)
                {
                    return _ServerOnline;
                }
            }
            set
            {
                lock (_ThreadLock)
                {
                    _ServerOnline = value;
                    if (!_ServerOnline)
                    {
                        _aTimer.Enabled = false;

                    }
                }
            }
        }

        /// <summary>
        /// Stops the Vaser Server
        /// </summary>
        public void Stop()
        {
            lock (_ThreadLock)
            {
                _ServerOnline = false;
                _aTimer.Enabled = false;

            }
        }

        /// <summary>
        /// Stops Vaser
        /// </summary>
        public static void StopEngine()
        {
            Options.Operating = false;
            _GCTimer.Enabled = false;
        }


        /// <summary>
        /// Starts listening for clients on selected Mode.
        /// </summary>
        public void Start()
        {
            try
            {
                _TCPListener.Start();
                
                _aTimer = new System.Timers.Timer(5);
                _aTimer.Elapsed += ListenForClients;
                _aTimer.AutoReset = true;
                _aTimer.Enabled = true;

                if (_GCTimer == null)
                {
                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;
                    _GCTimer = new System.Timers.Timer(15000);
                    _GCTimer.Elapsed += GC_Collect;
                    _GCTimer.AutoReset = true;
                    _GCTimer.Enabled = true;
                }
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
                lock (_ThreadLock)
                {
                    _ServerOption = VaserOptions.ModeNotEncrypted;
                    PColl._Active = true;
                    _PCollection = PColl;
                    _TCPListener = new TcpListener(LocalAddress, Port);
                    
                }
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
                lock (_ThreadLock)
                {
                    _vKerberos = Kerberos;
                    _ServerOption = VaserOptions.ModeKerberos;
                    PColl._Active = true;
                    _PCollection = PColl;
                    _TCPListener = new TcpListener(LocalAddress, Port);
                    
                }
            }catch(Exception ex)
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
                lock (_ThreadLock)
                {
                    _vSSL = SSL;
                    _ServerOption = VaserOptions.ModeSSL;
                    PColl._Active = true;
                    _PCollection = PColl;
                    _TCPListener = new TcpListener(LocalAddress, Port);
                    
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GC_Collect(Object source, System.Timers.ElapsedEventArgs e)
        {
            GC.Collect();
        }

        private void ListenForClients(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                while (_TCPListener.Pending())
                {

                    TcpClient Client = _TCPListener.AcceptTcpClient();
                    
                    ThreadPool.QueueUserWorkItem(QueueNewConnection, Client);

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR in VaserServer.ListenForClients() > " + ex.ToString());
            }

            if (!ServerOnline || !Options.Operating)
            {
                _aTimer.Enabled = false;

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
                Connection con = new Connection((TcpClient)Client, true, _ServerOption, _PCollection, _vKerberos, _vSSL, null, null, this);
                 
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

        internal void RemoveFromConnectionList(Connection con)
        {
            //Console.WriteLine("RemoveFromConnectionList called!");
            if (con == null) return;
            lock (_ConnectionList_ThreadLock)
            {
                if(_ConnectionList.Contains(con)) _ConnectionList.Remove(con);
            }

            lock (_DisconnectingLinkList_ThreadLock)
            {
                if (!_DisconnectingLinkList.Contains(con.link)) _DisconnectingLinkList.Add(con.link);
            }

            if (!DisconnectingQueueLock)
            {
                DisconnectingQueueLock = true;
                ThreadPool.QueueUserWorkItem(DisconnectingEventWorker);
            }
        }
        
        volatile bool NewQueueLock = false;
        //object _EventWorker_lock = new object();
        private void NewEventWorker(object threadContext)
        {

            lock (_NewLinkList_ThreadLock)
            {
                NewQueueLock = false;
                foreach (Link lnk in _NewLinkList)
                {
                    LinkEventArgs args = new LinkEventArgs();
                    args.lnk = lnk;

                    OnNewLink(args);
                }
                _NewLinkList.Clear();
            }
        }

        protected virtual void OnNewLink(LinkEventArgs e)
        {

            EventHandler<LinkEventArgs> handler = NewLink;
            if (handler != null)
            {
                //Debug.WriteLine("OnNewLink called!");
                handler(this, e);
            }
        }

        volatile bool DisconnectingQueueLock = false;
        //object _EventWorker_lock = new object();
        private void DisconnectingEventWorker(object threadContext)
        {
            List<Link> templist = null;

            lock (_DisconnectingLinkList_ThreadLock)
            {
                DisconnectingQueueLock = false;

                templist = _DisconnectingLinkList;
                _DisconnectingLinkList = new List<Link>();
            }

            foreach (Link lnk in templist)
            {
                lnk.Dispose();

                LinkEventArgs args = new LinkEventArgs();
                args.lnk = lnk;

                OnDisconnectingLink(args);
            }
            templist.Clear();

        }

        protected virtual void OnDisconnectingLink(LinkEventArgs e)
        {

            EventHandler<LinkEventArgs> handler = DisconnectingLink;
            if (handler != null)
            {
                //Debug.WriteLine("OnDisconnectingLink called!");
                handler(this, e);
            }
        }

        internal void AddNewLink(Link lnk)
        {
            lock (_NewLinkList_ThreadLock)
            {
                _NewLinkList.Add(lnk);
            }
            if (!NewQueueLock)
            {
                NewQueueLock = true;
                ThreadPool.QueueUserWorkItem(NewEventWorker);
            }

        }
    }

    public class LinkEventArgs : EventArgs
    {
        public Link lnk { get; set; }
    }
}
