using System;

namespace Q.Net
{
    public class HttpResponseHeader : HttpHeader
    {
        public string Status { get; private set; }

        public int StatusCode { get; private set; }

        public override string StartLine
        {
            get
            {
                return String.Format("{0} {1} {2}\r\n", this.Version, this.StatusCode, this.Status);
            }
        }

        public HttpResponseHeader(int statusCode, string status, string version = HttpVersion.Default)
        {
            this.StatusCode = statusCode;
            this.Status = status;
            this.Version = version;
        }
    }
}
