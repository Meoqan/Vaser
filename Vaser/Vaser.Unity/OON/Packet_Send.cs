namespace Vaser
{
    internal struct Packet_Send
    {
        internal byte[] _SendData;
        internal int Counter;
        internal bool _CallEmpybuffer;

        internal Packet_Send(byte[] Data, bool CallEmpybuffer)
        {
            _SendData = Data;
            Counter = 0;
            _CallEmpybuffer = CallEmpybuffer;
        }
    }
}
