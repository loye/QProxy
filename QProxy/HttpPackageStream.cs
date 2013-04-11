using System;
using System.IO;
using System.Net;
using Q.Http;

namespace Q.Proxy
{
    public class HttpPackageStream : Stream
    {
        public Guid Id { get; private set; }

        public Uri Url { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public Stream InnerStream { get; protected set; }

        public HttpPackageStream(string url, string host, int port, System.Net.IPEndPoint proxy = null)
        {
            Uri destination = new Uri(url);
            IPEndPoint endPoint = proxy != null ? proxy : new IPEndPoint(DnsHelper.GetHostAddress(destination.Host), destination.Port);

            this.Id = Guid.NewGuid();
            this.Url = destination;
            this.Host = host;
            this.Port = port;
            this.InnerStream = destination.Scheme == Uri.UriSchemeHttps
                ? new HttpsStream(endPoint, destination.Host, destination.Port, proxy != null)
                : new HttpStream(endPoint);
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

        public override int Read(byte[] buffer, int offset, int count)
        {



            return this.InnerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var httpHeader = this.NewRequestHeader();
            httpHeader.ContentLength = count;
            byte[] bin = httpHeader.ToBinary();
            this.InnerStream.Write(bin, 0, bin.Length);
            this.InnerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            this.InnerStream.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
