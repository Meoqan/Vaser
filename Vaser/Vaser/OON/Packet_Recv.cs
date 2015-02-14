using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Vaser
{
    public class Packet_Recv
    {
        public Link link;

        internal int ClassID = -1;
        public int ObjectID = -1;
        public int ContainerID = -1;
        internal long StreamPosition = -1;
        internal int PacketSize = -1;

        internal Packet_Recv(Link ilink, BinaryReader data)
        {
            link = ilink;

            extractIDs(data);
        }

        private void extractIDs(BinaryReader data)
        {
            ClassID = data.ReadInt32(); //Get Class ID
            ObjectID = data.ReadInt32(); //Get Object ID
            ContainerID = data.ReadInt32(); //Get Container ID
        }
    }
}
