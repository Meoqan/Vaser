using System.Threading;

namespace Vaser.OON
{
    public class cStatus
    {
        public uint TransmissionID = 0;

        AutoResetEvent SendDiscoverEvent = new AutoResetEvent(false);

        /// <summary>
        /// Link of this status
        /// </summary>
        public Link lnk
        {
            get; set;
        }
        /// <summary>
        /// Is true if the SetDone(...) or SetError(...) is called
        /// </summary>
        public bool Done
        {
            get; set;
        }
        /// <summary>
        /// Is true if the SetError(...) function is called
        /// </summary>
        public bool Error
        {
            get; set;
        }
        /// <summary>
        /// Error Message, set by SetError(...)
        /// </summary>
        public string Message
        {
            get; set;
        }
        /// <summary>
        /// Result object set by SetDone(...)
        /// </summary>
        public object ResultObject
        {
            get; set;
        }


        /// <summary>
        /// Wait for SetDone(...) or SetError(...)
        /// </summary>
        /// <returns></returns>
        public cStatus Wait()
        {
            if (Error == false && Done == false)
            {
                SendDiscoverEvent.WaitOne();
            }

            return this;
        }

        /// <summary>
        /// Sets the status to done and returns an ResultObject
        /// </summary>
        /// <param name="_ResultObject">Your Object</param>
        public void SetDone(object _ResultObject)
        {
            ResultObject = _ResultObject;

            Done = true;

            SendDiscoverEvent.Set();
        }

        /// <summary>
        /// Sets the status to done and error, returns an error messsage
        /// </summary>
        /// <param name="_Message"></param>
        public void SetError(string _Message)
        {
            Message = _Message;
            Error = true;
            Done = true;

            SendDiscoverEvent.Set();
        }

    }
}
