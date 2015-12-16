using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    /// <summary>
    /// This class is the portalholder.
    /// </summary>
    public class PortalCollection
    {
        private object _ListLock = new object();
        private Portal[] PortalArray = new Portal[256];

        /// <summary>
        /// Creates a new portal.
        /// </summary>
        /// <param name="ID">a channel ID you want to use</param>
        /// <returns>a portal</returns>
        public Portal CreatePortal(byte ID)
        {
            if (ID < 0 && ID > 255) throw new Exception("Portal ID must be between 0 and 255!");

            Portal port = new Portal(this, ID);

            lock (_ListLock)
            {
                if (PortalArray[ID] != null) throw new Exception("Portal ID is already used!");
                PortalArray[ID] = port;
            }

            return port;
        }
        
        internal object _GivePacketToClass_lock = new object();

        //internal static void lock_givePacketToClass() { _givePacketToClass_slimlock.Wait(); }
        //internal static void release_givePacketToClass() { _givePacketToClass_slimlock.Release(); }

        internal void GivePacketToClass(Packet_Recv pak, byte[] data)
        {
            if (pak.ClassID < 0 && pak.ClassID > 255) return;

            lock (_GivePacketToClass_lock)
            {
                Portal clas = PortalArray[pak.ClassID];
                if (clas != null)
                {
                    clas.AddPacket(pak, data);
                }
                else
                {
                    pak.link.Dispose();
                }
            }
        }
    }
}
