namespace Vaser
{
    /// <summary>
    /// Static settings collection
    /// </summary>
    public struct Options
    {
        internal static volatile bool Operating = true;


        /// <summary>
        /// Sets the maximum packet size. Default: 65007 (65000 + 7 PacketHeadSize)
        /// </summary>
        public static int MaximumPacketSize
        { get; set; } = 65007;

        /// <summary>
        /// Sets the packet head size. Default: 7
        /// </summary>
        public static int PacketHeadSize
        { get; set; } = 7;

        /// <summary>
        /// Enable the Heartbeat feature. Default: true
        /// </summary>
        public static bool EnableHeartbeat
        { get; set; } = true;

        /// <summary>
        /// Time between heartbeatpackets in milliseconds. Default: 60000
        /// </summary>
        public static int HeartbeatMilliseconds
        { get; set; } = 60000; // 60 sec
    }
}
