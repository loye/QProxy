using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace Q.Http
{
    public class HttpsStream : HttpStream
    {
        public HttpsStream(IPEndPoint endPoint, string host, int port, bool byProxy = false)
            : base(endPoint)
        {
            this.InnerStream = SwitchToSslStream(this.InnerStream, host, port, byProxy);
        }

        public HttpsStream(Socket socket, string host, int port, bool byProxy = false)
            : base(socket)
        {
            this.InnerStream = SwitchToSslStream(this.InnerStream, host, port, byProxy);
        }

        public HttpsStream(HttpStream stream, string host, int port, bool byProxy = false)
            : base(stream.Socket)
        {
            this.InnerStream = SwitchToSslStream(this.InnerStream, host, port, byProxy);
        }

        private SslStream SwitchToSslStream(Stream stream, string host, int port, bool connectToProxy)
        {
            SslStream ssltream = null;
            if (connectToProxy)
            {                
                HttpRequestHeader connectHeader = new HttpRequestHeader(HttpMethod.Connect, host, port);
                byte[] requestBin = connectHeader.ToBinary();
                stream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = new HttpReader().Read(stream);
                if (response == null || (response.HttpHeader as HttpResponseHeader).StatusCode != 200)
                {
                    throw new Exception(String.Format("SwitchToSslStream: Connect to proxy server[{0}:{1}] failed!", host, port));
                }
            }
            ssltream = new SslStream(stream, false);
            ssltream.AuthenticateAsClient(host);
            return ssltream;
        }
    }
}
