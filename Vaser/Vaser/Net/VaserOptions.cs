using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    public class VaserOptions
    {
        public static readonly VaserOptions ModeNotEncrypted = new VaserOptions(0);
        public static readonly VaserOptions ModeKerberos = new VaserOptions(1);
        public static readonly VaserOptions ModeSSL = new VaserOptions(2);

        internal int mode = 0;

        public VaserOptions(int i)
        {
            mode = i;
        }
    }
}
