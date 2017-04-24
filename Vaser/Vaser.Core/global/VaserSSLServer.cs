using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Vaser.ConnectionSettings
{
    /// <summary>
    /// This class provides security options for SSL encrypted server sockets.
    /// </summary>
    public class VaserSSLServer
    {
        /// <summary>
        /// Used by servers to authenticate the server and optionally the client in a client-server connection using the specified certificate.
        /// </summary>
        /// <param name="serverCertificate">The certificate used to authenticate the server.</param>
        public VaserSSLServer(X509Certificate2 serverCertificate)
        {
            _serverCertificate = serverCertificate;
        }

        /// <summary>
        /// Used by servers to authenticate the server and optionally the client in a client-server connection using the specified certificates, requirements and security protocol.
        /// </summary>
        /// <param name="serverCertificate">The X509Certificate used to authenticate the server.</param>
        /// <param name="clientCertificateRequired">A Boolean value that specifies whether the client is asked for a certificate for authentication. Note that this is only a request -- if no certificate is provided, the server still accepts the connection request.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        public VaserSSLServer(X509Certificate2 serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            _serverCertificate = serverCertificate;
            _clientCertificateRequired = clientCertificateRequired;
            _enabledSslProtocols = enabledSslProtocols;
            _checkCertificateRevocation = checkCertificateRevocation;
        }

        /// <summary>
        /// SERVER
        /// </summary>
        internal SslProtocols _enabledSslProtocols = SslProtocols.None;

        /// <summary>
        /// SERVER
        /// </summary>
        internal bool _checkCertificateRevocation = false;

        /// <summary>
        /// SERVER
        /// </summary>
        internal X509Certificate2 _serverCertificate = null;

        /// <summary>
        /// SERVER
        /// </summary>
        internal bool _clientCertificateRequired = false;

    }
}
