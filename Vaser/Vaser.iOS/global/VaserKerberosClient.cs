using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Authentication.ExtendedProtection;

namespace Vaser
{
    public class VaserKerberosClient
    {

        /// <summary>
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection.
        /// </summary>
        public VaserKerberosClient()
        {

        }

        /// <summary>
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified client credential.
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="targetName"></param>
        public VaserKerberosClient(NetworkCredential credential, string targetName)
        {
            _credential = credential;
            _targetName = targetName;
        }

        public VaserKerberosClient(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _credential = credential;
            _targetName = targetName;
            _requiredProtectionLevel = requiredProtectionLevel;
            _requiredImpersonationLevel = requiredImpersonationLevel;
        }
        
        /// <summary>
        /// CLIENT
        /// </summary>
        internal NetworkCredential _credential = null;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal ProtectionLevel _requiredProtectionLevel = ProtectionLevel.None;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal TokenImpersonationLevel _requiredImpersonationLevel = TokenImpersonationLevel.None;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal ProtectionLevel _allowedProtectionLevel = ProtectionLevel.None;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal string _targetName;

    }
}
