using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Q.Net;

namespace Q.Proxy.Net.Http
{
    public class HttpTunnelStream : Stream
    {
        private bool m_isHeaderRecieved;

        private MemoryStream m_recieveHeaderBuffer;

        public Uri HandlerUri { get; private set; }

        public string DestHost { get; private set; }

        public int DestPort { get; private set; }

        public Stream InnerStream { get; private set; }

        public HttpTunnelStream(string handler, string destHost, int destPort, IPEndPoint proxy = null)
        {
            this.HandlerUri = new Uri(handler);
            this.DestHost = destHost;
            this.DestPort = destPort;

            IPEndPoint endPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(this.HandlerUri.Host), this.HandlerUri.Port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);

            if (this.HandlerUri.Scheme == Uri.UriSchemeHttps)
            {
                stream = HttpsConnector.Instance.ConnectAsClientAsync(stream, this.HandlerUri.Host, this.HandlerUri.Port, proxy, true).WaitResult();
            }

            var httpHeader = this.NewRequestHeader();
            stream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);

            this.InnerStream = stream;

            Logger.Info(httpHeader.ToString());
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int lenght = 0;
            if (!m_isHeaderRecieved)
            {
                byte[] buf = new byte[1024];
                var mem = new MemoryStream();
                HttpHeader header = null;
                for (int len = this.InnerStream.Read(buf, 0, buf.Length); len > 0; len = this.InnerStream.Read(buf, 0, buf.Length))
                {
                    mem.Write(buf, 0, len);
                    if (HttpHeader.TryParse(mem.GetBuffer(), 0, (int)mem.Length, out header))
                    {
                        mem.Position = header.Length;
                        m_isHeaderRecieved = true;
                        m_recieveHeaderBuffer = mem;
                        Logger.Info(header.ToString());
                        break;
                    }
                }
                if (!m_isHeaderRecieved)
                {
                    return lenght;
                }
            }

            if (m_recieveHeaderBuffer != null)
            {
                int len1 = m_recieveHeaderBuffer.Read(buffer, offset, count);
                lenght += len1;
                if (len1 < count)
                {
                    m_recieveHeaderBuffer = null;
                }
            }
            else
            {
                lenght += this.InnerStream.Read(buffer, offset, count);
            }

            return lenght;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer != null && buffer.Length > 0)
            {
                byte[] chunked = buffer.ToChunked(offset, count);
                this.InnerStream.Write(chunked, 0, chunked.Length);
            }
        }

        private Q.Net.HttpRequestHeader NewRequestHeader()
        {
            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, this.HandlerUri.ToString(), this.HandlerUri.Host, this.HandlerUri.Port);
            httpHeader[HttpHeaderCustomKey.Type] = "HttpTunnel";
            httpHeader[HttpHeaderCustomKey.Host] = this.DestHost;
            httpHeader[HttpHeaderCustomKey.Port] = this.DestPort;
            httpHeader[HttpHeaderKey.Transfer_Encoding] = "chunked";
            return httpHeader;
        }

        #region Call InnerStream Methods

        public override bool CanRead
        {
            get { return this.InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this.InnerStream.CanWrite; }
        }

        public override long Length
        {
            get { return this.InnerStream.Length; }
        }

        public override long Position
        {
            get
            {
                return this.InnerStream.Position;
            }
            set
            {
                this.InnerStream.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.InnerStream.SetLength(value);
        }

        public override void Flush()
        {
            this.InnerStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            this.InnerStream.Write(new byte[0].ToChunked(), 0, 3);
            this.InnerStream.Flush();
            this.InnerStream.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
