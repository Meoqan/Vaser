using System.Diagnostics;

namespace Vaser.OON
{
    /// <summary>
    /// This is an channel class 'SendPacket -> IncommingPacket'
    /// for streaming data
    /// </summary>
    public class cChannel
    {
        internal Portal _Portal = null;
        internal Link _lnk1 = null;

        internal ushort ContainerID = 0;

        /// <summary>
        /// Free to use
        /// </summary>
        public object AttachedObject
        {
            get; set;
        }

        /// <summary>
        /// Process incoming packets from clients or server
        /// Usage:
        /// if (e.pak != null && mycon.UnpackContainer(e.pak, e.portal))
        /// {
        ///    Do stuff
        ///    SendPacket(myContainer);
        /// }
        /// </summary>
        /// <param name="p">Portal</param>
        /// <param name="e">PacketEventArgs</param>
        public virtual void IncomingPacket(object p, PacketEventArgs e)
        {
            /*if (e.pak != null &&con1.UnpackContainer(e.pak, e.portal))
            {
                Do stuff
            }*/

            Debug.WriteLine("IncomingPacket is not Implemented - closing link: " + e.lnk.IPv4Address);
            e.lnk.Dispose();
        }

        internal void ProcessPacket(object p, PacketEventArgs e)
        {
            _lnk1 = e.lnk;
            IncomingPacket(p, e);
            _lnk1 = null;
        }




        public void SendPacket(Container myContainer)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, 0, false);
        }
        public void SendPacket(Container myContainer, Link lnk)
        {
            Send(lnk, myContainer, 0, false);
        }
        public void SendPacket(Container myContainer, uint ObjectID)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, ObjectID, false);
        }
        public void SendPacket(Container myContainer, Link lnk, uint ObjectID)
        {
            Send(lnk, myContainer, ObjectID, false);
        }

        public void SendPacket(Container myContainer, bool _CallEmptybufferEvent)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, 0, _CallEmptybufferEvent);
        }
        public void SendPacket(Container myContainer, Link lnk, bool _CallEmptybufferEvent)
        {
            Send(lnk, myContainer, 0, _CallEmptybufferEvent);
        }
        public void SendPacket(Container myContainer, uint ObjectID, bool _CallEmptybufferEvent)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, ObjectID, _CallEmptybufferEvent);
        }
        public void SendPacket(Container myContainer, Link lnk, uint ObjectID, bool _CallEmptybufferEvent)
        {
            Send(lnk, myContainer, ObjectID, _CallEmptybufferEvent);
        }

        internal void Send(Link _lnk, Container _Con, uint ObjectID, bool _CallEmptybufferEvent)
        {
            _Portal.SendContainer(_lnk, _Con, ContainerID, ObjectID, _CallEmptybufferEvent);
        }


    }
}
