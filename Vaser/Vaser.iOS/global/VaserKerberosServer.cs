using System.Net;
using System.Net.Security;
using System.Security.Principal;

namespace Vaser.ConnectionSettings
{
    public class VaserKerberosServer
    {
        /// <summary>
        /// Called by servers to authenticate the client, and optionally the server, in a client-server connection.
        /// </summary>
        public VaserKerberosServer()
        {

        }

        /// <summary>
        /// Called by servers to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified server credentials and authentication options.
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="requiredProtectionLevel"></param>
        /// <param name="requiredImpersonationLevel"></param>
        public VaserKerberosServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _credential = credential;
            _requiredProtectionLevel = requiredProtectionLevel;
            _requiredImpersonationLevel = requiredImpersonationLevel;
        }

        /// <summary>
        /// SERVER
        /// </summary>
        internal NetworkCredential _credential = null;

        /// <summary>
        /// SERVER
        /// </summary>
        internal ProtectionLevel _requiredProtectionLevel = ProtectionLevel.None;

        /// <summary>
        /// SERVER
        /// </summary>
        internal TokenImpersonationLevel _requiredImpersonationLevel = TokenImpersonationLevel.None;
    }
}
