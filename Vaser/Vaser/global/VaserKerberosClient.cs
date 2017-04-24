using System.Net;
using System.Net.Security;
using System.Security.Principal;
using System.Security.Authentication.ExtendedProtection;

namespace Vaser.ConnectionSettings
{
    /// <summary>
    /// This class provides security options for Kerberos encrypted client connections.
    /// </summary>
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
        /// <param name="credential">The NetworkCredential that is used to establish the identity of the client.</param>
        /// <param name="targetName">The Service Principal Name (SPN) that uniquely identifies the server to authenticate.</param>
        public VaserKerberosClient(NetworkCredential credential, string targetName)
        {
            _credential = credential;
            _targetName = targetName;
        }

        /// <summary>
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified credentials and authentication options.
        /// </summary>
        /// <param name="credential">The NetworkCredential that is used to establish the identity of the client.</param>
        /// <param name="targetName">The Service Principal Name (SPN) that uniquely identifies the server to authenticate.</param>
        /// <param name="requiredProtectionLevel">One of the ProtectionLevel values, indicating the security services for the stream.</param>
        /// <param name="requiredImpersonationLevel">One of the TokenImpersonationLevel values, indicating how the server can use the client's credentials to access resources.</param>
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
        /// <param name="credential">The NetworkCredential that is used to establish the identity of the client.</param>
        /// <param name="binding">The ChannelBinding that is used for extended protection. </param>
        /// <param name="targetName">The Service Principal Name (SPN) that uniquely identifies the server to authenticate.</param>
        public VaserKerberosClient(NetworkCredential credential, ChannelBinding binding, string targetName)
        {
            _credential = credential;
            _targetName = targetName;
            _binding = binding;
        }

        /// <summary>
        /// Called by clients to authenticate the client, and optionally the server, in a client-server connection. The authentication process uses the specified credential, authentication options, and channel binding.
        /// </summary>
        /// <param name="credential">The NetworkCredential that is used to establish the identity of the client.</param>
        /// <param name="binding">The ChannelBinding that is used for extended protection.</param>
        /// <param name="targetName">The Service Principal Name (SPN) that uniquely identifies the server to authenticate.</param>
        /// <param name="requiredProtectionLevel">One of the ProtectionLevel values, indicating the security services for the stream.</param>
        /// <param name="requiredImpersonationLevel">One of the TokenImpersonationLevel values, indicating how the server can use the client's credentials to access resources.</param>
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
