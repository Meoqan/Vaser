using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace Vaser
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
