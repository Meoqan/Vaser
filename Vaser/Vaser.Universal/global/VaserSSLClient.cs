using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Vaser.ConnectionSettings
{
    /// <summary>
    /// This class provides security options for SSL encrypted client connections.
    /// </summary>
    public class VaserSSLClient
    {
        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection.
        /// </summary>
        /// <param name="targetHost">The name of the server that shares this SslStream.</param>
        public VaserSSLClient(string targetHost)
        {
            _targetHost = targetHost;
        }

        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection. The authentication process uses the specified certificate collection and SSL protocol.
        /// </summary>
        /// <param name="targetHost">The name of the server that will share this SslStream.</param>
        /// <param name="clientCertificates">The X509CertificateCollection that contains client certificates.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        public VaserSSLClient(string targetHost, X509Certificate2Collection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            _targetHost = targetHost;
            _clientCertificates = clientCertificates;
            _enabledSslProtocols = enabledSslProtocols;
            _checkCertificateRevocation = checkCertificateRevocation;
        }

        /// <summary>
        /// CLIENT
        /// </summary>
        internal string _targetHost = string.Empty;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal X509Certificate2Collection _clientCertificates = null;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal SslProtocols _enabledSslProtocols = SslProtocols.None;

        /// <summary>
        /// CLIENT
        /// </summary>
        internal bool _checkCertificateRevocation = false;

    }
}
