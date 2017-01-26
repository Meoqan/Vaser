using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Vaser.ConnectionSettings
{
    public class VaserSSLServer
    {
        public VaserSSLServer(X509Certificate2 serverCertificate)
        {
            _serverCertificate = serverCertificate;
        }

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
