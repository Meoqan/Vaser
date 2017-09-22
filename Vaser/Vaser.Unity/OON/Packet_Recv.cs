using System.IO;

namespace Vaser
{
    /// <summary>
    /// This class holds your incoming data packet.
    /// </summary>
    public struct Packet_Recv
    {
        /// <summary>
        /// The related link for this packet.
        /// </summary>
        public Link link;
        /// <summary>
        /// The object ID
        /// </summary>
        public uint ObjectID;

        /// <summary>
        /// The container ID
        /// </summary>
        public ushort ContainerID;

        internal byte ClassID;

        internal byte[] Data;


        //internal long StreamPosition = -1;
        //internal int PacketSize = -1;

        internal Packet_Recv(Link ilink, BinaryReader data)
        {
            link = ilink;
            ObjectID = 0;
            ContainerID = 0;
            ClassID = 0;
            ClassID = data.ReadByte(); //Get Class ID Len: 1 B
            ObjectID = data.ReadUInt32(); //Get Object ID Len: 4 B
            ContainerID = data.ReadUInt16(); //Get Container ID Len: 2 B
            Data = null;
        }
    }
}
