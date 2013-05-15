using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Q.Http;

namespace Q.Proxy
{
    public class HttpPackageStream : Stream
    {
        public Guid Id { get; private set; }

        public Uri Url { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public NetworkStream InnerStream { get; protected set; }

        public HttpPackageStream(string url, string host, int port, IPEndPoint proxy = null)
        {
            Uri destination = new Uri(url);
            IPEndPoint endPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(destination.Host), destination.Port);

            this.Id = Guid.NewGuid();
            this.Url = destination;
            this.Host = host;
            this.Port = port;

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            NetworkStream networkStream = new NetworkStream(socket, true);
            if (destination.Scheme == Uri.UriSchemeHttps)
            {
                if (proxy != null)
                {
                    var requestHeader = new Http.HttpRequestHeader(HttpMethod.Connect, destination.Host, destination.Port);
                    HttpPackage response = HttpPackage.Parse(networkStream);
                    if (response == null || (response.HttpHeader as Http.HttpResponseHeader).StatusCode != 200)
                    {
                        throw new Exception(String.Format("Connect to proxy server[{0}:{1}] with SSL failed!", requestHeader.Host, requestHeader.Port));
                    }
                }
                SslStream ssltream = new SslStream(networkStream, false);
                ssltream.AuthenticateAsClient(destination.Host);
            }
            this.InnerStream = networkStream;
        }

        private Stream Connect(string host, int port, IPEndPoint proxy)
        {
            return null;
        }

      
        private Http.HttpRequestHeader NewRequestHeader()
        {
            var httpHeader = new Http.HttpRequestHeader(HttpMethod.POST, this.Url.ToString(), this.Url.Host, this.Url.Port);
            httpHeader[HttpHeaderCustomKey.Id] = this.Id.ToString();
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
