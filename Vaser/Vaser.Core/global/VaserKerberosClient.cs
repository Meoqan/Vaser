using System.Net;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace Vaser.ConnectionSettings
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
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified client credential and the channel binding.
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="binding"></param>
        /// <param name="targetName"></param>
        public VaserKerberosClient(NetworkCredential credential, ChannelBinding binding, string targetName)
        {
            _credential = credential;
            _targetName = targetName;
            _binding = binding;
        }

        /// <summary>
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified credential, authentication options, and channel binding.
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="binding"></param>
        /// <param name="targetName"></param>
        /// <param name="requiredProtectionLevel"></param>
        /// <param name="requiredImpersonationLevel"></param>
        public VaserKerberosClient(NetworkCredential credential, ChannelBinding binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _credential = credential;
            _targetName = targetName;
            _requiredProtectionLevel = requiredProtectionLevel;
            _requiredImpersonationLevel = requiredImpersonationLevel;
            _binding = binding;
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
        internal ChannelBinding _binding = null;

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
