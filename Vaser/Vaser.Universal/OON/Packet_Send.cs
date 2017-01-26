namespace Vaser
{
    internal class Packet_Send
    {
        internal byte[] _SendData = null;
        internal int Counter = 0;
        internal bool _CallEmpybuffer = false;

        internal Packet_Send(byte[] Data, bool CallEmpybuffer)
        {
            _SendData = Data;
            _CallEmpybuffer = CallEmpybuffer;
        }
    }
}
