namespace Vaser
{
    /// <summary>
    /// Simple dataholder.
    /// </summary>
    public class NetVector2
    {
        public float[] Data = { 0.0f, 0.0f };

        public NetVector2()
        {

        }

        public NetVector2(float fm1, float fm2)
        {
            Data[0] = fm1;
            Data[1] = fm2;
        }
    }
}
