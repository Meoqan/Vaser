using System.Net;
using System.Net.Security;
using System.Security.Principal;
using System.Security.Authentication.ExtendedProtection;

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
        public VaserKerberosServer(NetworkCredential credential, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _credential = credential;
            _requiredImpersonationLevel = requiredImpersonationLevel;
        }
        
        /// <summary>
        /// SERVER
        /// </summary>
        internal NetworkCredential _credential = null;
        
        /// <summary>
        /// SERVER
        /// </summary>
        internal TokenImpersonationLevel _requiredImpersonationLevel = TokenImpersonationLevel.None;
    }
}
