using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
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

        public async Task<Stream> ConnectAsClientAsync(IPEndPoint serverEndPoint, string host, int port, bool decryptSSL, bool httpProxy = false)
        {
            Socket socket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverEndPoint);
            Stream serverStream = new NetworkStream(socket, true);
            return await ConnectAsClientAsync(serverStream, host, port, decryptSSL, httpProxy);
        }

        public async Task<Stream> ConnectAsClientAsync(Stream serverStream, string host, int port, bool decryptSSL, bool httpProxy = false)
        {
            if (httpProxy)
            {
                // Send connect request to http proxy server
                byte[] requestBin = new Q.Net.HttpRequestHeader(HttpMethod.Connect, host, port).ToBinary();
                serverStream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = HttpPackage.Parse(serverStream);
                if (response == null || (response.HttpHeader as Net.HttpResponseHeader).StatusCode != 200)
                {
                    throw new Exception(String.Format("Connect to proxy server[{0}:{1}] with SSL failed!", host, port));
                }
            }
            // Decrypt SSL
            if (decryptSSL)
            {
                serverStream = await SwitchToSslStreamAsClientAsync(serverStream, host);
            }
            return serverStream;
        }

        public async Task<Stream> ConnectAsServerAsync(Stream clientStream, string host, bool decryptSSL)
        {
            // Send connected response to client
            byte[] responseBin = new Q.Net.HttpResponseHeader(200, Q.Net.HttpStatus.Connection_Established).ToBinary();
            clientStream.Write(responseBin, 0, responseBin.Length);
            // Decrypt SSL
            if (decryptSSL)
            {
                clientStream = await SwitchToSslStreamAsServerAsync(clientStream, host);
            }
            return clientStream;
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
