using System;

namespace QProxy
{
    public class HttpResponseHeader : HttpHeader
    {
        public string Status { get; private set; }

        public int StatusCode { get; private set; }

        public override string StartLine
        {
            get
            {
                return String.Format("{0} {1}\r\n", this.Status, this.Version);
            }
        }

        public HttpResponseHeader(int statusCode, string status, string version = "HTTP/1.1")
        {
            this.StatusCode = statusCode;
            this.Status = status;
            this.Version = version;
        }
    }
}
