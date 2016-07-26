using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    /// <summary>
    /// This class holds encryptionsmodes for vaser connections.
    /// </summary>
    public class VaserOptions
    {
        /// <summary>
        /// This mode is used for unsafe unencrypted connection. All data will be tranmitted read- and changealbe.
        /// </summary>
        public static readonly VaserOptions ModeNotEncrypted = new VaserOptions(0);
        /// <summary>
        /// This mode is used in typical internal domain environments. It's one of the strongest and simplest encryptions for internal use.
        /// </summary>
        public static readonly VaserOptions ModeKerberos = new VaserOptions(1);
        /// <summary>
        /// This mode uses SSL TLS 1.2 for trusted internet connections. Vaser can't connect to invalid or self signed certificates.
        /// </summary>
        public static readonly VaserOptions ModeSSL = new VaserOptions(2);

        internal int mode = 0;

        internal VaserOptions(int i)
        {
            mode = i;
        }
    }
}
