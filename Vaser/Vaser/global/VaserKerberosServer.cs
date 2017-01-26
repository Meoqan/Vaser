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
        /// Called by servers to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified extended protection policy.
        /// </summary>
        /// <param name="policy"></param>
        public VaserKerberosServer(ExtendedProtectionPolicy policy)
        {
            _policy = policy;
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
        /// Called by servers to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified server credentials, authentication options, and extended protection policy.
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="policy"></param>
        /// <param name="requiredProtectionLevel"></param>
        /// <param name="requiredImpersonationLevel"></param>
        public VaserKerberosServer(NetworkCredential credential, ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _credential = credential;
            _requiredProtectionLevel = requiredProtectionLevel;
            _requiredImpersonationLevel = requiredImpersonationLevel;
            _policy = policy;
        }

        /// <summary>
        /// SERVER
        /// </summary>
        internal ExtendedProtectionPolicy _policy = null;

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
