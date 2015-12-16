using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Vaser
{
    /// <summary>
    /// This class holds your incoming data packet.
    /// </summary>
    public class Packet_Recv
    {
        /// <summary>
        /// The related link for this packet.
        /// </summary>
        public Link link;
        /// <summary>
        /// The object ID
        /// </summary>
        public uint ObjectID = 0;

        /// <summary>
        /// The container ID
        /// </summary>
        public ushort ContainerID = 0;

        internal byte ClassID = 0;

        
        
        internal long StreamPosition = -1;
        internal int PacketSize = -1;

        internal Packet_Recv(Link ilink, BinaryReader data)
        {
            link = ilink;

            extractIDs(data);
        }

        private void extractIDs(BinaryReader data)
        {
            ClassID = data.ReadByte(); //Get Class ID Len: 1 B
            ObjectID = data.ReadUInt32(); //Get Object ID Len: 4 B
            ContainerID = data.ReadUInt16(); //Get Container ID Len: 2 B
        }
    }
}
