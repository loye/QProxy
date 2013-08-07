using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Q.Net;

namespace Q.Net
{
    public class HttpStream : Stream
    {
        public Guid Id { get; private set; }

        public Uri HandlerUri { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public Stream InnerStream { get; protected set; }

        public HttpStream(Uri handlerUri, string host, int port, IPEndPoint proxy = null)
        {
            IPEndPoint endPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(handlerUri.Host), handlerUri.Port);

            this.Id = Guid.NewGuid();
            this.HandlerUri = handlerUri;
            this.Host = host;
            this.Port = port;

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);
            if (handlerUri.Scheme == Uri.UriSchemeHttps)
            {
                if (proxy != null)
                {
                    var requestHeader = new Net.HttpRequestHeader(HttpMethod.Connect, handlerUri.Host, handlerUri.Port);
                    HttpPackage response = HttpPackage.Parse(stream);
                    if (response == null || (response.HttpHeader as Net.HttpResponseHeader).StatusCode != 200)
                    {
                        throw new Exception(String.Format("Connect to proxy server[{0}:{1}] with SSL failed!", requestHeader.Host, requestHeader.Port));
                    }
                }
                SslStream ssltream = new SslStream(stream, false);
                ssltream.AuthenticateAsClient(handlerUri.Host);
                stream = ssltream;
            }
            this.InnerStream = stream;
        }

        private Net.HttpRequestHeader NewRequestHeader()
        {
            var httpHeader = new Net.HttpRequestHeader(HttpMethod.POST, this.HandlerUri.ToString(), this.HandlerUri.Host, this.HandlerUri.Port);
            //httpHeader[HttpHeaderCustomKey.Id] = this.Id.ToString();
            httpHeader[HttpHeaderCustomKey.Host] = this.Host;
            httpHeader[HttpHeaderCustomKey.Port] = this.Port;
            return httpHeader;
        }


        #region implements of Stream

        public override int Read(byte[] buffer, int offset, int count)
        {




            return this.InnerStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0 && offset + count <= buffer.Length)
            {
                var httpHeader = this.NewRequestHeader();
                httpHeader.ContentLength = count;
                byte[] bin = httpHeader.ToBinary();
                this.InnerStream.Write(bin, 0, bin.Length);
                this.InnerStream.Write(buffer, offset, count);
            }
        }

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

        public override void Flush()
        {
            this.InnerStream.Flush();
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

        protected override void Dispose(bool disposing)
        {
            this.InnerStream.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
