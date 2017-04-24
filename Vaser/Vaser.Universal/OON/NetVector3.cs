namespace Vaser
{
    /// <summary>
    /// Simple dataholder.
    /// </summary>
    class NetVector3
    {
        public float[] Data = { 0.0f, 0.0f, 0.0f };

        public NetVector3()
        {

        }

        public NetVector3(float fm1, float fm2, float fm3)
        {
            Data[0] = fm1;
            Data[1] = fm2;
            Data[2] = fm3;
        }
    }
}
