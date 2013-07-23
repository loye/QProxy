using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Q.Net
{
    public class HttpRequestHeader : HttpHeader
    {
        private static readonly Regex REGEX_URL = new Regex(@"^(?:(?<schema>http|https|(?<schemaNotSupported>\w+))\://)?(?<host>[^/: ]+)?(?:\:(?<port>\d+))?\S*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string m_host;

        public string HttpMethod { get; private set; }

        public string Url { get; private set; }

        public string Host
        {
            get
            {
                return String.IsNullOrEmpty(m_host) ? (string)this[HttpHeaderKey.Host] : m_host;
            }
            private set
            {
                m_host = value;
            }
        }

        public int Port { get; private set; }

        public override string StartLine
        {
            get
            {
                return String.Format("{0} {1} {2}\r\n", this.HttpMethod, this.Url, this.Version);
            }
        }

        public HttpRequestHeader(string httpMethod, string host, int port, string version = HttpVersion.Default) :
            this(httpMethod, host + ":" + port, host, port, version)
        {
        }

        public HttpRequestHeader(string httpMethod, string url, string host, int port, string version = HttpVersion.Default)
        {
            this.HttpMethod = httpMethod;
            this.Url = url;
            this.Host = host;
            this.Port = port;
            this.Version = version;
        }

        public override string ToString()
        {
            if (m_rawString == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.StartLine);
                if (this.HeaderItemCount(HttpHeaderKey.Host) == 0 && this.Host != null)
                {

                    sb.Append(this.Port == 80
                        ? String.Format("{0}: {1}\r\n", HttpHeaderKey.Host, this.Host)
                        : String.Format("{0}: {1}:{2}\r\n", HttpHeaderKey.Host, this.Host, this.Port));
                }
                foreach (var item in this)
                {
                    sb.Append(item.ToString());
                }
                sb.Append("\r\n");
                m_rawString = sb.ToString();
            }
            return m_rawString;
        }
    }
}
