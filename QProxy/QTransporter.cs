using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace QProxy
{
    public class QTransporter : IDisposable
    {
        private HttpRequestHeader m_httpHeader;


        public Guid Id { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public bool IsByProxy { get; private set; }

        public bool IsSecure { get; private set; }

        public Socket Socket { get; private set; }

        public Stream Stream { get; private set; }


        public QTransporter(string url, IPEndPoint proxy = null)
        {
            m_httpHeader = new HttpRequestHeader(HttpMethod.POST, url);

            this.Id = Guid.NewGuid();
            this.EndPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(m_httpHeader.Host), m_httpHeader.Port);
            this.IsByProxy = proxy != null;
            this.IsSecure = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);
            this.Socket = new Socket(this.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            m_httpHeader[HttpHeaderKey.SCV_Id] = this.Id.ToString();
        }

        public QTransporter Connect()
        {
            if (this.Socket.Connected)
            {
                this.Socket.Disconnect(true);
            }
            this.Socket.Connect(this.EndPoint);
            this.Stream = this.IsSecure
                ? SwitchToSslStream(new NetworkStream(Socket), m_httpHeader.Host, m_httpHeader.Port, this.IsByProxy)
                : new NetworkStream(Socket) as Stream;
            return this;
        }

        public void Send(byte[] buffer, int offset, int count)
        {


            var stream = this.Stream;
            this.Stream.Write(buffer, 0, buffer.Length);
            this.Stream.Flush();
        }

        public int Receive()
        {

            return 0;
        }


        public void Dispose()
        {
            if (this.Stream != null)
            {
                this.Stream.Dispose();
            }
            if (this.Socket != null)
            {
                this.Socket.Dispose();
            }
        }

        private SslStream SwitchToSslStream(Stream stream, string host, int port, bool isConnectToProxy)
        {
            SslStream ssltream = null;
            if (isConnectToProxy)
            {
                HttpRequestHeader connectHeader = new HttpRequestHeader(HttpMethod.Connect, host, port);
                HttpPackage connectPackage = new HttpPackage(connectHeader, HttpContent.Empty);
                byte[] requestBin = connectPackage.ToBinary();
                stream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = HttpPackage.Read(stream);
            }
            ssltream = new SslStream(stream, false);
            ssltream.AuthenticateAsClient(host);
            return ssltream;
        }
    }
}
