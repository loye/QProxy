using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Q.Net;

namespace Q.Proxy.Net.Http
{
    public class HttpTunnelStream : Stream
    {
        private bool m_isHeaderSent;

        private bool m_isHeaderRecieved;

        private MemoryStream m_recieveHeaderBuffer;

        public Uri HandlerUri { get; private set; }

        public string DestHost { get; private set; }

        public int DestPort { get; private set; }

        public Stream InnerStream { get; private set; }

        public HttpTunnelStream(Uri handlerUri, string destHost, int destPort, IPEndPoint proxy = null)
        {
            IPEndPoint endPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(handlerUri.Host), handlerUri.Port);

            this.HandlerUri = handlerUri;
            this.DestHost = destHost;
            this.DestPort = destPort;

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);
            if (handlerUri.Scheme == Uri.UriSchemeHttps)
            {
                var task = HttpsConnector.Instance.ConnectAsClientAsync(stream, handlerUri.Host, handlerUri.Port, proxy, true);
                task.Wait();
                stream = task.Result;
            }
            this.InnerStream = stream;
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
                        break;
                    }
                }
            }

            if (m_recieveHeaderBuffer != null)
            {
                int len1 = m_recieveHeaderBuffer.Read(buffer, offset, count);
                lenght += len1;
                if (len1 < count)
                {
                    m_recieveHeaderBuffer = null;
                    lenght += this.InnerStream.Read(buffer, offset + len1, count - len1);
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
            if (!m_isHeaderSent)
            {
                var httpHeader = this.NewRequestHeader();
                byte[] bin = httpHeader.ToBinary();
                this.InnerStream.Write(bin, 0, bin.Length);
                m_isHeaderSent = true;
            }

            this.InnerStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            this.InnerStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            this.InnerStream.Dispose();
            base.Dispose(disposing);
        }

        private Q.Net.HttpRequestHeader NewRequestHeader()
        {
            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, this.HandlerUri.ToString(), this.HandlerUri.Host, this.HandlerUri.Port);
            httpHeader[HttpHeaderKey.Connection] = "closed";
            httpHeader[HttpHeaderCustomKey.Host] = this.DestHost;
            httpHeader[HttpHeaderCustomKey.Port] = this.DestPort;
            return httpHeader;
        }

        #region Not Supported Methods

        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
