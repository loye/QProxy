using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Q.Net
{
    public class HttpTunnelStream : Stream
    {
        public string ID { get; set; }

        public Uri HandlerUri { get; private set; }

        public IPEndPoint Proxy { get; set; }

        public string DestHost { get; private set; }

        public int DestPort { get; private set; }

        public Stream WriteStream { get; private set; }

        public Stream ReadStream { get; private set; }


        public HttpTunnelStream(string handler, string destHost, int destPort, IPEndPoint proxy = null)
        {
            this.ID = Guid.NewGuid().ToString("N");
            this.HandlerUri = new Uri(handler);
            this.Proxy = proxy;
            this.DestHost = destHost;
            this.DestPort = destPort;
            this.WriteStream = this.Connect();
            this.ReadStream = this.Connect();

            var httpHeader = this.NewRequestHeader(HttpHeaderCustomValue.Action.Connect);
            httpHeader[HttpHeaderCustomKey.Host] = this.DestHost;
            httpHeader[HttpHeaderCustomKey.Port] = this.DestPort;
            this.WriteStream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);

            HttpPackage package = RecievePackage(this.WriteStream);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int lenght = 0;
            if (this.ReadStream == null)
            {
                Console.WriteLine("null");
                this.ReadStream = this.Connect();
            }
            var httpHeader = this.NewRequestHeader(HttpHeaderCustomValue.Action.Read);
            httpHeader[HttpHeaderCustomKey.Length] = count;
            this.ReadStream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);
            HttpPackage package = RecievePackage(this.ReadStream);
            byte[] bin = package.HttpContent.ToBinary();
            lenght = bin.Length;
            Array.Copy(bin, 0, buffer, offset, lenght);
            return lenght;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.WriteStream == null)
            {
                Console.WriteLine("null");
                this.WriteStream = this.Connect();
            }
            var httpHeader = this.NewRequestHeader(HttpHeaderCustomValue.Action.Write);
            httpHeader.ContentLength = count;
            this.WriteStream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);
            this.WriteStream.Write(buffer, offset, count);
            HttpPackage package = RecievePackage(this.WriteStream);
        }


        private Stream Connect()
        {
            IPEndPoint endPoint = this.Proxy != null ? this.Proxy : new IPEndPoint(DnsHelper.GetHostAddress(this.HandlerUri.Host), this.HandlerUri.Port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);
            if (this.HandlerUri.Scheme == Uri.UriSchemeHttps)
            {
                stream = HttpsConnector.Instance.ConnectAsClientAsync(stream, this.HandlerUri.Host, this.HandlerUri.Port, this.Proxy, true).WaitResult();
            }
            return stream;
        }

        private Q.Net.HttpRequestHeader NewRequestHeader(string action)
        {
            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, this.HandlerUri.ToString(), this.HandlerUri.Host, this.HandlerUri.Port);
            httpHeader[HttpHeaderCustomKey.ID] = this.ID;
            httpHeader[HttpHeaderCustomKey.Action] = action;
            httpHeader.ContentLength = 0;
            return httpHeader;
        }

        private HttpPackage RecievePackage(Stream stream)
        {
            HttpPackage package = HttpPackage.Parse(stream);
            if (package == null)
            {
                throw new Exception("HttpPackage is null");
            }
            if (package.HttpHeader[HttpHeaderCustomKey.Exception] != null)
            {
                throw new Exception(String.Format("Remote Expection: [{0}]\r\n{1}", package.HttpHeader[HttpHeaderCustomKey.Exception].ToString().Trim(), package.HttpContent.ToString()));
            }
            return package;
        }


        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            if (this.WriteStream != null)
            {
                this.WriteStream.Flush();
            }
            if (this.ReadStream != null)
            {
                this.ReadStream.Flush();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (this.WriteStream != null)
            {
                this.WriteStream.Dispose();
            }
            if (this.ReadStream != null)
            {
                this.ReadStream.Dispose();
            }
            try
            {
                var httpHeader = this.NewRequestHeader(HttpHeaderCustomValue.Action.Close);
                Stream stream = this.Connect();
                stream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);
                HttpPackage package = RecievePackage(stream);
                stream.Dispose();
            }
            catch (Exception)
            {
            }
            base.Dispose(disposing);
        }

        public void Debug()
        {
            var time1 = DateTime.Now;
            var httpHeader = this.NewRequestHeader(HttpHeaderCustomValue.Action.Debug);
            this.WriteStream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);
            HttpPackage package = RecievePackage(this.WriteStream);

            Console.WriteLine(package.HttpHeader[HttpHeaderCustomKey.Message] + " [Time: " + (DateTime.Now - time1) + "]");

        }

        #region Not supported

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    /*
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
                Logger.Info(ASCIIEncoding.ASCII.GetString(chunked, 0, chunked.Length));
            }
        }

        private Q.Net.HttpRequestHeader NewRequestHeader()
        {
            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, this.HandlerUri.ToString(), this.HandlerUri.Host, this.HandlerUri.Port);
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
    */
}
