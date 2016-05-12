using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;

namespace VaserUnitTest
{
    // Build your data container
    public class TestContainer : Container
    {
        //only public, nonstatic and standard datatypes can be transmitted
        public int ID = 1;
        public string test = "test text!";

        //also 1D arrays are posible
        public int[] array = new int[1000];
    }
}
