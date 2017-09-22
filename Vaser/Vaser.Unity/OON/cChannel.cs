using System.Diagnostics;

namespace Vaser.OON
{
    /// <summary>
    /// The cChannel class provides a design pattern for continuous streaming data.
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
        /// Process incoming packets from a clients or server.
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



        /// <summary>
        /// Sends a packet back the source.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        public void SendPacket(Container myContainer)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, 0,false);
        }

        /// <summary>
        /// Sends a packet to a link.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="lnk">The target link.</param>
        public void SendPacket(Container myContainer, Link lnk)
        {
            Send(lnk, myContainer, 0, false);
        }
        /// <summary>
        /// Sends a packet back the source.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="ObjectID">Sets the ObjectID.</param>
        public void SendPacket(Container myContainer, uint ObjectID)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, ObjectID, false);
        }

        /// <summary>
        /// Sends a packet to a link.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="lnk">The target link.</param>
        /// <param name="ObjectID">Sets the ObjectID.</param>
        public void SendPacket(Container myContainer, Link lnk, uint ObjectID)
        {
            Send(lnk, myContainer, ObjectID, false);
        }
        /// <summary>
        /// Sends a packet back the source. Can raise an EmptybufferEvent.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="_CallEmptybufferEvent">If true, the link raises an EmptybufferEvent.</param>
        public void SendPacket(Container myContainer, bool _CallEmptybufferEvent)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, 0, _CallEmptybufferEvent);
        }
        /// <summary>
        /// Sends a packet to a link. Can raise an EmptybufferEvent.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="lnk">The target link.</param>
        /// <param name="_CallEmptybufferEvent">If true, the link raises an EmptybufferEvent.</param>
        public void SendPacket(Container myContainer, Link lnk, bool _CallEmptybufferEvent)
        {
            Send(lnk, myContainer, 0, _CallEmptybufferEvent);
        }
        /// <summary>
        /// Sends a packet back the source. Can raise an EmptybufferEvent.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="ObjectID">Sets the ObjectID.</param>
        /// <param name="_CallEmptybufferEvent">If true, the link raises an EmptybufferEvent.</param>
        public void SendPacket(Container myContainer, uint ObjectID, bool _CallEmptybufferEvent)
        {
            if (_lnk1 == null) throw new System.Exception("Link '_lnk1' is null");
            Send(_lnk1, myContainer, ObjectID, _CallEmptybufferEvent);
        }
        /// <summary>
        ///  Sends a packet to a link. Can raise an EmptybufferEvent.
        /// </summary>
        /// <param name="myContainer">The datacontainer for transmission, can be null.</param>
        /// <param name="lnk">The target link.</param>
        /// <param name="ObjectID">Sets the ObjectID.</param>
        /// <param name="_CallEmptybufferEvent">If true, the link raises an EmptybufferEvent.</param>
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
