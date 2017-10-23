using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Vaser
{
    /// <summary>
    /// Event data holder
    /// </summary>
    public class PacketEventArgs : EventArgs
    {
        /// <summary>
        /// Link of the event.
        /// </summary>
        public Link lnk { get; set; }
        /// <summary>
        /// Packet of the event.
        /// </summary>
        public Packet_Recv pak { get; set; }
        /// <summary>
        /// Portal of the event.
        /// </summary>
        public Portal portal { get; set; }
    }

    /// <summary>
    /// This class is a data gateway for sending and receiving packets.
    /// It helps to manage the datastream by separating the packets by its thematic.
    /// </summary>
    public class Portal
    {
        internal byte _PID = 0;

        //internal PortalCollection _PCollection;
        internal Dictionary<ushort, OON.cRequest> RequestDictionary = new Dictionary<ushort, OON.cRequest>();
        internal Dictionary<ushort, OON.cChannel> ChannelDictionary = new Dictionary<ushort, OON.cChannel>();

        internal int counter = 0;

        /// <summary>
        /// EventHandler for incoming packets.
        /// </summary>
        public event EventHandler<PacketEventArgs> IncomingPacket;

        internal List<Packet_Recv> packetList1 = new List<Packet_Recv>();

        internal MemoryStream _sendMS = null;
        internal BinaryWriter _sendBW = null;

        int PacketHeadSize = Options.PacketHeadSize;

        /// <summary>
        /// Creates a new portal. Please register it at 'MyPortalCollection.RegisterPortal(...)'.
        /// </summary>
        /// <param name="PID">The portal ID (Range 0-255)</param>
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

        internal void RegisterRequest(ushort ContainerID, OON.cRequest _Request)
        {
            RequestDictionary.Add(ContainerID, _Request);
        }

        internal void RegisterChannel(ushort ContainerID, OON.cChannel _Channel)
        {
            ChannelDictionary.Add(ContainerID, _Channel);
        }

        internal void RemoveDisconectingLinkFromRequests(Link _lnk)
        {
            foreach (OON.cRequest r in RequestDictionary.Values)
            {
                r.RemoveDisconnectedLink(_lnk);
            }
        }

        volatile bool QueueLock = false;
        object _EventWorker_lock = new object();
        OON.cChannel channel = null;
        OON.cRequest request = null;
        PacketEventArgs args = null;
        private void EventWorker(object threadContext)
        {
            List<Packet_Recv> templist = GetPakets();
            while (templist.Count != 0)
            {
                //Debug.WriteLine("EventWorker");
                foreach (Packet_Recv paks in templist)
                {
                    if (!paks.link.Connect.StreamIsConnected) break; //Stop processing packets when client is disconnected

                    args = new PacketEventArgs
                    {
                        lnk = paks.link,
                        pak = paks,
                        portal = this
                    };

                    if (ChannelDictionary.TryGetValue(paks.ContainerID, out channel))
                    {
                        //Console.WriteLine("RequestDictionary");
                        channel.ProcessPacket(this, args);
                    }
                    else
                    {
                        // wenn con id in request liste dann
                        if (RequestDictionary.TryGetValue(paks.ContainerID, out request))
                        {
                            //Console.WriteLine("RequestDictionary");
                            request.ProcessPacket(this, args);
                        }
                        else
                        {
                            //Console.WriteLine("OnIncomingPacket");
                            OnIncomingPacket(args);
                        }
                    }

                }
                templist.Clear();
                templist = GetPakets();
            }

        }

        /// <summary>
        /// Raises an event when a new packet has arrived.
        /// </summary>
        /// <param name="e">The packet data.</param>
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

            //Operating threadsave
            lock (SendContainer_lock)
            {
                if (lnk.IsConnected == false || _sendBW == null) return;


                // write databody
                counter = 0;
                if (con != null)
                {
                    _sendMS.Position = PacketHeadSize + 4;

                    Packet_Send spacket = con.PackContainer(_sendBW, _sendMS);
                    //big datapacket dedected
                    /*if (_sendMS.Position >= Options.MaximumPacketSize + 4)
                    {
                        return;
                    }
                    else
                    {*/
                        counter = (ushort)_sendMS.Position - 4;
                    //}


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
                            spacket._SendData = _sendMS.ToArray();
                            spacket._CallEmpybuffer = CallEmptyBufferEvent;

                            lnk.SendDataPortalArray[_PID].Enqueue(spacket);
                            lnk.Connect.QueueSend();
                        }
                    }
                }
                else
                {
                    counter += PacketHeadSize;

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
                            Packet_Send spacket = new Packet_Send(_sendMS.ToArray(), CallEmptyBufferEvent);

                            lnk.SendDataPortalArray[_PID].Enqueue(spacket);
                            lnk.Connect.QueueSend();
                        }
                    }
                }

                //reset 
                _sendMS.SetLength(0);
                //_sendMS.Flush();

            }

        }

        /// <summary>
        /// Forward a packet unread further to another client or server.
        /// </summary>
        /// <param name="lnk">The target link.</param>
        /// <param name="packet">The unread packet.</param>
        public void DispatchContainer(Link lnk, Packet_Recv packet)
        {
            //Operating threadsave
            lock (SendContainer_lock)
            {
                if (lnk.IsConnected == false || _sendBW == null) return;

                _sendMS.Position = 0;
                if (_sendBW != null)
                {
                    if (packet.Data == null)
                    {
                        _sendBW.Write(PacketHeadSize);

                        _sendBW.Write(this._PID);
                        _sendBW.Write(packet.ObjectID);
                        _sendBW.Write(packet.ContainerID);
                    }
                    else
                    {
                        _sendBW.Write(packet.Data.Length + PacketHeadSize);

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

        /// <summary>
        /// Converts an bytearray to an string.
        /// </summary>
        /// <param name="ba">The data.</param>
        /// <returns>A string.</returns>
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }


}
