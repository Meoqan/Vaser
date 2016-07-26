using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace Vaser
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
