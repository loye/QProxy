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
                stream = HttpsConnector.Instance.ConnectAsClientAsync(stream, this.HandlerUri.Host, this.HandlerUri.Port, true, this.Proxy != null).WaitResult();
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
}
