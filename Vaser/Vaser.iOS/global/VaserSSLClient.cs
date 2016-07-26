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

namespace Vaser
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
