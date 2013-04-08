using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using Q.Http;

namespace Q.Proxy
{
    public class Transporter : IDisposable
    {
        private HttpRequestHeader m_httpHeader;

        public Guid Id { get; private set; }

        public bool SSL { get; private set; }

        public Stream Stream { get; private set; }

        public Transporter(string url, string host, int port, System.Net.IPEndPoint proxy = null)
        {
            m_httpHeader = new HttpRequestHeader(HttpMethod.POST, url);
            var endPoint = proxy != null ? proxy : new System.Net.IPEndPoint(DnsHelper.GetHostAddress(m_httpHeader.Host), m_httpHeader.Port);

            this.Id = Guid.NewGuid();
            bool byProxy = proxy != null;
            this.SSL = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);
            this.Stream = this.SSL
                ? new HttpsStream(endPoint, m_httpHeader.Host, m_httpHeader.Port, byProxy)
                : new HttpStream(endPoint);

            m_httpHeader[HttpHeaderCustomKey.Id] = this.Id.ToString();
        }

        public void Send(byte[] buffer, int offset, int count)
        {


            //var stream = this.Stream;
            //this.Stream.Write(buffer, 0, buffer.Length);
            //this.Stream.Flush();
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
        }
    }
}
