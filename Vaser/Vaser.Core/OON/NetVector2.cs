namespace Vaser
{
    /// <summary>
    /// A simple dataholder.
    /// </summary>
    public class NetVector2
    {
        /// <summary>
        /// The data.
        /// </summary>
        public float[] Data = { 0.0f, 0.0f };

        /// <summary>
        /// A simple dataholder.
        /// </summary>
        public NetVector2()
        {

        }
        /// <summary>
        /// A simple dataholder.
        /// </summary>
        /// <param name="fm1">X-Axis</param>
        /// <param name="fm2">Y-Axis</param>
        public NetVector2(float fm1, float fm2)
        {
            Data[0] = fm1;
            Data[1] = fm2;
        }
    }
}
