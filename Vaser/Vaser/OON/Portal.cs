﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Security;
using System.IO;
using Vaser.global;
using System.Diagnostics;

namespace Vaser
{
    public class Portal
    {
        internal int Cpos = 0;
        private static List<Portal> CList = new List<Portal>();



        internal int counter = 0;

        //MemoryStream big_data_sms = null;
        //BinaryWriter big_data_sbw = null;

        //MemoryStream sms = null;
        //BinaryWriter sbw = null;

        private List<Packet_Recv> packetList1 = new List<Packet_Recv>();
        internal MemoryStream rms1 = null;
        BinaryWriter rbw1 = null;
        internal BinaryReader rbr1 = null;

        private List<Packet_Recv> packetListTEMP = null;
        internal MemoryStream rmsTEMP = null;
        BinaryWriter rbwTEMP = null;
        internal BinaryReader rbrTEMP = null;

        private List<Packet_Recv> packetList2 = new List<Packet_Recv>();
        internal MemoryStream rms2 = null;
        BinaryWriter rbw2 = null;
        internal BinaryReader rbr2 = null;

        public Portal()
        {
            //big_data_sms = new MemoryStream();
            //big_data_sbw = new BinaryWriter(big_data_sms);

            //sms = new MemoryStream();
            //sbw = new BinaryWriter(sms);

            rms1 = new MemoryStream();
            rbw1 = new BinaryWriter(rms1);
            rbr1 = new BinaryReader(rms1);

            rms2 = new MemoryStream();
            rbw2 = new BinaryWriter(rms2);
            rbr2 = new BinaryReader(rms2);

            Cpos = CList.Count;
            CList.Add(this);
        }

        internal static object _givePacketToClass_slimlock = new object();

        //internal static void lock_givePacketToClass() { _givePacketToClass_slimlock.Wait(); }
        //internal static void release_givePacketToClass() { _givePacketToClass_slimlock.Release(); }

        internal static void givePacketToClass(Packet_Recv pak, byte[] data)
        {
            lock (_givePacketToClass_slimlock)
            {
                Portal clas = CList[pak.ClassID];
                clas.packetList1.Add(pak);
                pak.StreamPosition = clas.rms1.Position;
                if (data != null)
                {
                    pak.PacketSize = data.Length;
                    clas.rbw1.Write(data);
                }
                else
                {
                    pak.PacketSize = 0;
                }
            }
        }

        /// <summary>
        /// Get all new received data packets.
        /// </summary>
        /// <returns>a list of all packets</returns>
        public List<Packet_Recv> GetPakets()
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


            lock (_givePacketToClass_slimlock)
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
        /// Send data to the client.
        /// </summary>
        /// <param name="lnk">the link to the client</param>
        /// <param name="con">the container you want to send. can be null.</param>
        /// <param name="ContainerID">manually set</param>
        /// <param name="ObjectID">manually set</param>
        public void SendContainer(Link lnk, Container con, int ContainerID, int ObjectID)
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

                    lnk.bw.Write(this.Cpos);
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
        /// Flush the databuffer and send all data to the clients/server.
        /// </summary>
        public static void Finialize()
        {
            List<Link> tempLinkList = Link.LinkList.ToList<Link>();
            foreach (Link lnk in tempLinkList)
            {
                if (lnk != null) lnk.SendData();
            }
            tempLinkList.Clear();
        }
    }
}
