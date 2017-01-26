using System.Security.Authentication;
using Windows.Security.Cryptography.Certificates;

namespace Vaser.ConnectionSettings
{
    public class VaserSSLClient
    {
        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection.
        /// </summary>
        /// <param name="targetHost"></param>
        public VaserSSLClient(string targetHost)
        {
            _targetHost = targetHost;
        }

        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection. The authentication process uses the specified certificate collection and SSL protocol.
        /// </summary>
        /// <param name="targetHost"></param>
        /// <param name="clientCertificates"></param>
        /// <param name="enabledSslProtocols"></param>
        /// <param name="checkCertificateRevocation"></param>
        public VaserSSLClient(string targetHost, Certificate clientCertificate, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            _targetHost = targetHost;
            _clientCertificate = clientCertificate;
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
        internal Certificate _clientCertificate = null;

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
