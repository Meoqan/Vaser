using System.Security.Authentication;
using Windows.Security.Cryptography.Certificates;

namespace Vaser.ConnectionSettings
{
    public class VaserSSLServer
    {
        public VaserSSLServer(Certificate serverCertificate)
        {
            _serverCertificate = serverCertificate;
        }

        public VaserSSLServer(Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
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
        internal Certificate _serverCertificate = null;

        /// <summary>
        /// SERVER
        /// </summary>
        internal bool _clientCertificateRequired = false;

    }
}
