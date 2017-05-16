using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vaser.OON;

namespace Vaser
{
    /// <summary>
    /// This class is the portalholder.
    /// </summary>
    public class PortalCollection
    {
        private object _ListLock = new object();
        internal bool _Active = false;

        internal bool Active
        {
            get
            {
                return _Active;
            }
            set
            {
                _Active = value;
            }
        }

        internal Portal[] PortalArray = new Portal[256];

        /// <summary>
        /// Register an portal on this collection.
        /// The collection should not be used before.
        /// </summary>
        /// <param name="portal">The portal you want to register.</param>
        public void RegisterPortal(Portal portal)
        {
            if(Active) throw new Exception("this PortalCollection is in use! please register portals before using");

            lock (_ListLock)
            {
                if (PortalArray[portal._PID] != null) throw new Exception("Portal ID is already used!");
                PortalArray[portal._PID] = portal;
            }
        }

        /// <summary>
        /// Register an request class for packetprocessing
        /// </summary>
        /// <param name="RequestHandler">An initialized request class.</param>
        /// <param name="portal">The portal.</param>
        /// <param name="ContainerID">The container ID.</param>
        public void RegisterRequest(cRequest RequestHandler, Portal portal, ushort ContainerID)
        {
            if (Active) throw new Exception("this PortalCollection is in use! please register portals before using");
            RequestHandler._Portal = portal;
            RequestHandler.ContainerID = ContainerID;
            portal.RegisterRequest(ContainerID, RequestHandler);
        }

        /// <summary>
        /// Register an channel class for packetprocessing
        /// </summary>
        /// <param name="ChannelHandler">An initialized channel class.</param>
        /// <param name="portal">The portal.</param>
        /// <param name="ContainerID">The container ID.</param>
        public void RegisterChannel(cChannel ChannelHandler, Portal portal, ushort ContainerID)
        {
            if (Active) throw new Exception("this PortalCollection is in use! please register portals before using");
            ChannelHandler._Portal = portal;
            ChannelHandler.ContainerID = ContainerID;
            portal.RegisterChannel(ContainerID, ChannelHandler);
        }

        internal object _GivePacketToClass_lock = new object();

        internal void GivePacketToClass(Packet_Recv pak)
        {

            Portal clas = PortalArray[pak.ClassID];
            if (clas != null)
            {
                clas.AddPacket(pak);
            }
            else
            {
                Debug.WriteLine("Vaser.Portal> Portal not found.");
                pak.link.Dispose();
            }

        }

        internal void GivePacketToClass(List<Packet_Recv> Lpak)
        {

            foreach (Packet_Recv pak in Lpak)
            {
                Portal clas = PortalArray[pak.ClassID];
                if (clas != null)
                {
                    clas.AddPacket(pak);
                }
                else
                {
                    Debug.WriteLine("Vaser.Portal> Portal not found.");
                    pak.link.Dispose();
                }
            }

        }

        internal void RemoveDisconectingLinkFromRequest(Link _lnk)
        {
            foreach(Portal p in PortalArray)
            {
                if(p != null) p.RemoveDisconectingLinkFromRequests(_lnk);
            }
        }
    }
}
