using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Q.Http
{
    [Obsolete]
    public class HttpStream : Stream
    {
        public Socket Socket { get; private set; }

        public Stream InnerStream { get; protected set; }

        public HttpStream(IPEndPoint endPoint)
        {
            this.Socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.Connect(endPoint);
            this.InnerStream = new NetworkStream(this.Socket);
        }

        public HttpStream(Socket socket)
        {
            this.Socket = socket;
            this.InnerStream = new NetworkStream(this.Socket);
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
