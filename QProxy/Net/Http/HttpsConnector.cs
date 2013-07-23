using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Q.Net
{
    public class HttpsConnector
    {
        private static HttpsConnector m_instance;

        private static object locker = new object();

        private HttpsConnector() { }

        public static HttpsConnector Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new HttpsConnector();
                        }
                    }
                }
                return m_instance;
            }
        }

        public async Task<Stream> ConnectAsClientAsync(Stream remoteStream, string host, int port, IPEndPoint proxy, bool decryptSSL)
        {
            // Send connect request to http proxy server
            if (proxy != null)
            {
                byte[] requestBin = new Net.HttpRequestHeader(HttpMethod.Connect, host, port).ToBinary();
                remoteStream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = HttpPackage.Parse(remoteStream);
                if (response == null || (response.HttpHeader as Net.HttpResponseHeader).StatusCode != 200)
                {
                    throw new Exception(String.Format("Connect to proxy server[{0}:{1}] with SSL failed!", host, port));
                }
            }
            // Decrypt SSL
            if (decryptSSL)
            {
                remoteStream = await SwitchToSslStreamAsClientAsync(remoteStream, host);
            }
            return remoteStream;
        }

        public async Task<Stream> ConnectAsServerAsync(Stream localStream, string host, bool decryptSSL)
        {
            // Send connected response to local
            byte[] responseBin = new Net.HttpResponseHeader(200, Net.HttpStatus.Connection_Established).ToBinary();
            localStream.Write(responseBin, 0, responseBin.Length);
            // Decrypt SSL
            if (decryptSSL)
            {
                localStream = await SwitchToSslStreamAsServerAsync(localStream, host);
            }
            return localStream;
        }

        private async Task<SslStream> SwitchToSslStreamAsClientAsync(Stream stream, string host)
        {
            SslStream ssltream = new SslStream(stream, false);
            await ssltream.AuthenticateAsClientAsync(host);
            return ssltream;
        }

        private async Task<SslStream> SwitchToSslStreamAsServerAsync(Stream stream, string host)
        {
            SslStream sslStream = new SslStream(stream, false);
            await sslStream.AuthenticateAsServerAsync(CAHelper.GetCertificate(host), false, SslProtocols.Tls, true);
            return sslStream;
        }
    }
}
