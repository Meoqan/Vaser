﻿using System.Net;
using System.Net.Security;
using System.Security.Principal;

namespace Vaser.ConnectionSettings
{
    /// <summary>
    /// This class provides security options for Kerberos encrypted server sockets.
    /// </summary>
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
        /// <param name="credential">The NetworkCredential that is used to establish the identity of the server.</param>
        /// <param name="requiredProtectionLevel">One of the ProtectionLevel values, indicating the security services for the stream.</param>
        /// <param name="requiredImpersonationLevel">One of the TokenImpersonationLevel values, indicating how the server can use the client's credentials to access resources.</param>
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
