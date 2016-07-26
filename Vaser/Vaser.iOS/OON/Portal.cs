using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Security;
using System.IO;
using System.Diagnostics;

namespace Vaser
{
    /// <summary>
    /// This class is a data gateway for sending and receiving packets.
    /// It helps to manage the datastream by separating the packets by its thematic.
    /// </summary>
    public class Portal
    {
        internal byte _PID = 0;

        //internal PortalCollection _PCollection;

        internal int counter = 0;

        /// <summary>
        /// EventHandler for incoming packets.
        /// </summary>
        public event EventHandler<PacketEventArgs> IncomingPacket;

        internal List<Packet_Recv> packetList1 = new List<Packet_Recv>();

        internal MemoryStream _sendMS = null;
        internal BinaryWriter _sendBW = null;

        /// <summary>
        /// Creates a new portal. Please register it at 'MyPortalCollection.RegisterPortal(...)'.
        /// </summary>
        /// <param name="PColl"></param>
        /// <param name="PID"></param>
        public Portal(byte PID)
        {

            _sendMS = new MemoryStream();
            _sendBW = new BinaryWriter(_sendMS);

            _PID = PID;
        }

        //internal Queue<List<Packet_Recv>> QueueList = new Queue<List<Packet_Recv>>();
        internal object _AddPacket_lock = new object();


        internal void AddPacket(Packet_Recv pak)
        {
            //operating Threadsafe
            lock (_AddPacket_lock)
            {
                packetList1.Add(pak);
                //PacketQueue.Enqueue(pak);

                if (!QueueLock)
                {
                    QueueLock = true;
                    ThreadPool.QueueUserWorkItem(EventWorker);
                }
            }



        }

        volatile bool QueueLock = false;
        object _EventWorker_lock = new object();
        private void EventWorker(object threadContext)
        {
            //operating Threadsafe
            lock (_EventWorker_lock)
            {
                List<Packet_Recv> templist = GetPakets();
                while (templist.Count != 0)
                {
                    //Debug.WriteLine("EventWorker");
                    foreach (Packet_Recv pak in templist)
                    {
                        PacketEventArgs args = new PacketEventArgs();
                        args.lnk = pak.link;
                        args.pak = pak;
                        args.portal = this;
                        OnIncomingPacket(args);
                    }
                    templist = GetPakets();
                }

                //templist.Clear();
            }
        }

        protected virtual void OnIncomingPacket(PacketEventArgs e)
        {
            //Debug.WriteLine("OnIncomingPacket");
            IncomingPacket?.Invoke(this, e);
        }

        /// <summary>
        /// Get all new received data packets.
        /// </summary>
        /// <returns>a list of all packets</returns>
        internal List<Packet_Recv> GetPakets()
        {
            //packetList2.Clear();
            List<Packet_Recv> packetListTEMP = null;

            lock (_AddPacket_lock)
            {

                packetListTEMP = packetList1;
                packetList1 = new List<Packet_Recv>();
                if (packetListTEMP.Count == 0) QueueLock = false;
            }

            return packetListTEMP;
        }


        private object SendContainer_lock = new object();

        /// <summary>
        /// Send data packets to the client.
        /// </summary>
        /// <param name="lnk">the link to the client</param>
        /// <param name="con">the container you want to send. can be null.</param>
        /// <param name="ContainerID">manually set</param>
        /// <param name="ObjectID">manually set</param>
        /// <param name="CallEmptyBufferEvent">if true raise an event</param>
        public void SendContainer(Link lnk, Container con, ushort ContainerID, uint ObjectID, bool CallEmptyBufferEvent = false)
        {
            try
            {
                //Operating threadsave
                lock (SendContainer_lock)
                {
                    if (lnk.IsConnected == false || _sendBW == null) return;


                    // write databody
                    counter = 0;
                    Packet_Send spacket = null;
                    if (con != null)
                    {
                        _sendMS.Position = Options.PacketHeadSize + 4;

                        spacket = con.PackContainer(_sendBW, _sendMS);
                        //big datapacket dedected
                        if (_sendMS.Position >= Options.MaximumPacketSize + 4)
                        {
                            return;
                        }
                        else
                        {
                            counter = (ushort)_sendMS.Position - 4;
                        }
                    }
                    else
                    {
                        counter += Options.PacketHeadSize;
                    }

                    //write header
                    _sendMS.Position = 0;

                    _sendBW.Write(counter);

                    _sendBW.Write(this._PID);
                    _sendBW.Write(ObjectID);
                    _sendBW.Write(ContainerID);



                    //Operating threadsave
                    lock (lnk.SendData_Lock)
                    {
                        if (lnk.SendDataPortalArray[_PID] != null)
                        {
                            if (spacket == null)
                            {
                                spacket = new Packet_Send(_sendMS.ToArray(), CallEmptyBufferEvent);
                            }
                            else
                            {
                                spacket._SendData = _sendMS.ToArray();
                                spacket._CallEmpybuffer = CallEmptyBufferEvent;
                            }

                            lnk.SendDataPortalArray[_PID].Enqueue(spacket);
                            lnk.Connect.QueueSend();
                        }
                    }

                    //reset 
                    _sendMS.SetLength(0);
                    //_sendMS.Flush();

                }
            }
            catch (Exception es)
            {
                Debug.WriteLine("Portal.SendContainer()  > " + es.ToString());
            }
        }

        public void DispatchContainer(Link lnk, Packet_Recv packet)
        {
            try
            {
                //Operating threadsave
                lock (SendContainer_lock)
                {
                    if (lnk.IsConnected == false || _sendBW == null) return;

                    _sendMS.Position = 0;
                    if (_sendBW != null)
                    {
                        if (packet == null)
                        {
                            _sendBW.Write(Options.PacketHeadSize);

                            _sendBW.Write(this._PID);
                            _sendBW.Write(packet.ObjectID);
                            _sendBW.Write(packet.ContainerID);
                        }
                        else
                        {
                            _sendBW.Write(packet.Data.Length + Options.PacketHeadSize);

                            _sendBW.Write(this._PID);
                            _sendBW.Write(packet.ObjectID);
                            _sendBW.Write(packet.ContainerID);

                            _sendBW.Write(packet.Data);
                        }
                    }

                    //Operating threadsave
                    lock (lnk.SendData_Lock)
                    {
                        if (lnk.SendDataPortalArray[_PID] != null)
                        {

                            Packet_Send spacket = new Packet_Send(_sendMS.ToArray(), false);

                            lnk.SendDataPortalArray[_PID].Enqueue(spacket);
                            lnk.Connect.QueueSend();

                        }
                    }

                    //reset 
                    _sendMS.SetLength(0);
                    //_sendMS.Flush();

                }
            }
            catch (Exception es)
            {
                Debug.WriteLine("Portal.SendContainer()  > " + es.ToString());
            }
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }

    public class PacketEventArgs : EventArgs
    {
        public Link lnk { get; set; }
        public Packet_Recv pak { get; set; }
        public Portal portal { get; set; }
    }
}
