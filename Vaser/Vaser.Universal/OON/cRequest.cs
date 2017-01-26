using System.Collections.Generic;
using System.Diagnostics;

namespace Vaser.OON
{
    public class cRequest
    {
        internal cStatus Status = null;
        internal Portal _Portal = null;
        internal Link _lnk1 = null;
        internal uint _TransmissionID = 0;

        internal ushort ContainerID = 0;
        internal static uint StaticTransmissionID = 0;

        internal Dictionary<uint, cStatus> StatusDictionary = new Dictionary<uint, cStatus>();

        /// <summary>
        /// Free to use
        /// </summary>
        public object AttachedObject
        {
            get; set;
        }

        List<uint> RemList = new List<uint>();

        internal void RemoveDisconnectedLink(Link _lnk)
        {
            lock(StatusDictionary)
            {
                foreach(cStatus s in StatusDictionary.Values)
                {
                    if(s.lnk == _lnk)
                    {
                        RemList.Add(s.TransmissionID);
                        s.SetError("Disconnected");
                    }
                }

                foreach(uint i in RemList)
                {
                    StatusDictionary.Remove(i);
                }
                RemList.Clear();
            }
        }


        /// <summary>
        /// Process incoming packets from clients or server
        /// Usage:
        /// if (e.pak != null && mycon.UnpackContainer(e.pak, e.portal))
        /// {
        ///    Do stuff
        ///    RequestResponse(mycon);
        /// }else{ RequestResponse(null); } // send empty response
        /// </summary>
        /// <param name="p">Portal</param>
        /// <param name="e">PacketEventArgs</param>
        public virtual void IncomingRequest(object p, PacketEventArgs e)
        {
            /*if (con1.UnpackContainer(e.pak, e.portal))
            {
                Do stuff

                send answer

                RequestResponse(mycon);
            }*/

            //RequestResponse(null); // or maybe close link?

            Debug.WriteLine("IncomingRequest is not Implemented - closing link: " + e.lnk.IPv4Address);
            e.lnk.Dispose();
        }

        /// <summary>
        /// Process incoming result packets from clients or server
        /// Usage:
        /// if (e.pak != null && mycon.UnpackContainer(e.pak, e.portal))
        /// {
        ///    Do stuff
        ///    SetDone(myObject);
        /// }else{ SetError("myError"); }
        /// </summary>
        /// <param name="p">Portal</param>
        /// <param name="e">PacketEventArgs</param>
        public virtual void RequestResult(object p, PacketEventArgs e)
        {
            // do stuff

            SetError("Decode error.");
        }

        internal void ProcessPacket(object p, PacketEventArgs e)
        {
            _lnk1 = e.lnk;
            lock (StatusDictionary)
            {
                if (StatusDictionary.TryGetValue(e.pak.ObjectID, out Status))
                {
                    if (_lnk1 == e.pak.link)
                    {
                        RequestResult(p, e);
                    }
                    else
                    {
                        // this request is not yours!
                        e.pak.link.Dispose();
                    }

                }
                else
                {
                    _TransmissionID = e.pak.ObjectID;
                    IncomingRequest(p, e);
                }
            }
        }

        /// <summary>
        /// Start an reqeust and wait for an response
        /// </summary>
        /// <param name="lnk">Link</param>
        /// <param name="myContainer">Data container</param>
        /// <param name="CallEmptyBuffer">Raise an "call empty bufffer" event</param>
        /// <returns>Status object</returns>
        public cStatus StartRequest(Link lnk, Container myContainer, bool CallEmptyBuffer = false)
        {
            cStatus nStatus = StatusFactory(lnk);

            lock (StatusDictionary)
            {
                StatusDictionary.Add(nStatus.TransmissionID, nStatus);
            }

            SendPacket(lnk, myContainer, nStatus, CallEmptyBuffer);

            return nStatus;
        }

        /// <summary>
        /// Send an Response of a request
        /// </summary>
        /// <param name="myContainer">Data container</param>
        /// <param name="CallEmptyBuffer">Raise an "call empty bufffer" event</param>
        public void RequestResponse(Container myContainer, bool CallEmptyBuffer = false)
        {
            SendPacket(_lnk1, myContainer, _TransmissionID, CallEmptyBuffer);
        }

        /// <summary>
        /// Sets the status to done and returns an ResultObject
        /// </summary>
        /// <param name="_ResultObject">Your Object</param>
        public void SetDone(object _ResultObject)
        {
            if (Status != null)
            {
                lock (StatusDictionary)
                {
                    if (StatusDictionary.ContainsKey(Status.TransmissionID)) StatusDictionary.Remove(Status.TransmissionID);
                }
                Status.SetDone(_ResultObject);
            }
        }

        /// <summary>
        /// Sets the status to done and error, returns an error messsage
        /// </summary>
        /// <param name="_Message"></param>
        public void SetError(string _Message)
        {
            if (Status != null)
            {
                lock (StatusDictionary)
                {
                    if (StatusDictionary.ContainsKey(Status.TransmissionID)) StatusDictionary.Remove(Status.TransmissionID);
                }
                Status.SetError(_Message);
            }
        }

        cStatus StatusFactory(Link _lnk)
        {
            cStatus nStatus = new cStatus();

            StaticTransmissionID++;
            nStatus.TransmissionID = StaticTransmissionID;
            nStatus.lnk = _lnk;

            Status = nStatus;

            return nStatus;
        }

        void SendPacket(Link _lnk, Container _Con, uint _TransmissionID, bool CallEmptyBufferEvent)
        {
            _Portal.SendContainer(_lnk, _Con, ContainerID, _TransmissionID, CallEmptyBufferEvent);
        }

        void SendPacket(Link _lnk, Container _Con, cStatus _Status, bool CallEmptyBufferEvent)
        {
            //Console.WriteLine("SendPacket ContainerID: " + ContainerID + "  TransmissionID:" + _Status.TransmissionID);
            _Portal.SendContainer(_lnk, _Con, ContainerID, _Status.TransmissionID, CallEmptyBufferEvent);
        }
        
    }
}
