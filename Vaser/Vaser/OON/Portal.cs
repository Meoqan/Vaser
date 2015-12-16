﻿using System;
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
        
        internal PortalCollection _PCollection;

        internal int counter = 0;

        //MemoryStream big_data_sms = null;
        //BinaryWriter big_data_sbw = null;

        //MemoryStream sms = null;
        //BinaryWriter sbw = null;

        /// <summary>
        /// EventHandler for incoming packets.
        /// </summary>
        public event EventHandler<PacketEventArgs> IncomingPacket;

        internal List<Packet_Recv> packetList1 = new List<Packet_Recv>();
        internal MemoryStream rms1 = null;
        internal BinaryWriter rbw1 = null;
        internal BinaryReader rbr1 = null;

        internal List<Packet_Recv> packetListTEMP = null;
        internal MemoryStream rmsTEMP = null;
        internal BinaryWriter rbwTEMP = null;
        internal BinaryReader rbrTEMP = null;

        internal List<Packet_Recv> packetList2 = new List<Packet_Recv>();
        internal MemoryStream rms2 = null;
        internal BinaryWriter rbw2 = null;
        internal BinaryReader rbr2 = null;

        /// <summary>
        /// Creates a new portal. Please use 'MyPortalCollection.CreatePortal(...)' instead.
        /// </summary>
        /// <param name="PColl"></param>
        /// <param name="PID"></param>
        public Portal(PortalCollection PColl, byte PID)
        {
            if (PColl == null) throw new Exception("A PortalCollection is required. Please use <PortalCollection>.CreatePortal(ID) for creating portals.");

            rms1 = new MemoryStream();
            rbw1 = new BinaryWriter(rms1);
            rbr1 = new BinaryReader(rms1);

            rms2 = new MemoryStream();
            rbw2 = new BinaryWriter(rms2);
            rbr2 = new BinaryReader(rms2);

            _PCollection = PColl;
            _PID = PID;
        }


        internal object _AddPacket_lock = new object();
        internal void AddPacket(Packet_Recv pak, byte[] data)
        {
            lock (_AddPacket_lock)
            {
                packetList1.Add(pak);
                pak.StreamPosition = rms1.Position;
                if (data != null)
                {
                    pak.PacketSize = data.Length;
                    rbw1.Write(data);
                }
                else
                {
                    pak.PacketSize = 0;
                }
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
            
            lock (_EventWorker_lock)
            {
                QueueLock = false;
                foreach (Packet_Recv pak in GetPakets())
                {
                    PacketEventArgs args = new PacketEventArgs();
                    args.lnk = pak.link;
                    args.pak = pak;
                    args.portal = this;
                    OnIncomingPacket(args);
                }
            }
        }

        protected virtual void OnIncomingPacket(PacketEventArgs e)
        {
            
            EventHandler<PacketEventArgs> handler = IncomingPacket;
            if (handler != null)
            {
                //Debug.WriteLine("OnIncomingPacket called!");
                handler(this, e);

            }
        }

        /// <summary>
        /// Get all new received data packets.
        /// </summary>
        /// <returns>a list of all packets</returns>
        internal List<Packet_Recv> GetPakets()
        {
            packetList2.Clear();

            if (rms2.Length < 10000000)
            {
                rms2.SetLength(0);
                rms2.Flush();
                rbw2.Flush();
            }
            else
            {
                rms2.Dispose();
                rbw2.Dispose();
                rbr2.Dispose();
                rms2 = new MemoryStream();
                rbw2 = new BinaryWriter(rms2);
                rbr2 = new BinaryReader(rms2);
                //GC.Collect();
            }
            //rms2.SetLength(0);
            //rms2.Flush();
            //rbw2.Flush();


            //switch the packetstream
            packetListTEMP = packetList2;
            rmsTEMP = rms2;
            rbwTEMP = rbw2;
            rbrTEMP = rbr2;


            lock (_AddPacket_lock)
            {

                packetList2 = packetList1;
                rms2 = rms1;
                rbw2 = rbw1;
                rbr2 = rbr1;

                packetList1 = packetListTEMP;
                rms1 = rmsTEMP;
                rbw1 = rbwTEMP;
                rbr1 = rbrTEMP;

            }


            rms2.Position = 0;

            return packetList2;
        }

        /// <summary>
        /// Send data packets to the client.
        /// </summary>
        /// <param name="lnk">the link to the client</param>
        /// <param name="con">the container you want to send. can be null.</param>
        /// <param name="ContainerID">manually set</param>
        /// <param name="ObjectID">manually set</param>
        public void SendContainer(Link lnk, Container con, ushort ContainerID, uint ObjectID)
        {
            if (lnk.IsConnected == false || lnk.bw == null) return;
            Packet_Send pak = null;
            if (con != null)
            {
                pak = con.PackContainer();
                //big datapacket dedected
                if (pak.Counter >= Options.MaximumPacketSize)
                {
                    return;
                }
                else
                {
                    counter = (ushort)pak.Counter;
                }
            }

            counter += Options.PacketHeadSize;

            lock (lnk.SendData_Lock)
            {

                if (lnk.bw != null)
                {
                    lnk.bw.Write(counter);

                    lnk.bw.Write(this._PID);
                    lnk.bw.Write(ObjectID);
                    lnk.bw.Write(ContainerID);


                    if (con != null)
                    {
                        lnk.bw.Write(pak.SendData);
                        //Debug.WriteLine("Protal.SendContainer byte wirtten: " + pak.SendData.Length);
                    }
                }
            }


            //big_data_sms.SetLength(0);
            //big_data_sms.Flush();
            //big_data_sbw.Flush();

            counter = 0;
        }

        /// <summary>
        /// Flush the databuffer and send all data to the client/server.
        /// </summary>
        public static void Finialize()
        {
            Link.SendAllData();
        }
    }

    public class PacketEventArgs : EventArgs
    {
        public Link lnk { get; set; }
        public Packet_Recv pak { get; set; }
        public Portal portal { get; set; }
    }
}