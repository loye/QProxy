using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Q.Http
{
    public class HttpRequestHeader : HttpHeader
    {
        private static readonly Regex REGEX_URL = new Regex(@"^(?:(?<schema>http|https|(?<schemaNotSupported>\w+))\://)?(?<host>[^/: ]+)?(?:\:(?<port>\d+))?\S*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string HttpMethod { get; private set; }

        public string Url { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public override string StartLine
        {
            get
            {
                return String.Format("{0} {1} {2}\r\n", this.HttpMethod, this.Url, this.Version);
            }
        }
        /*
        public HttpRequestHeader(string httpMethod, string url, string version = "HTTP/1.1")
        {
            Match match = REGEX_URL.Match(url);
            if (!match.Success || match.Groups["schemaNotSupported"].Success)
            {
                throw new ArgumentException("url incorrect!", "url");
            }
            string host = match.Groups["host"].Success ? match.Groups["host"].Value : null;
            int port = match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value)
                        : (!match.Groups["schema"].Success ? 80
                            : (String.Compare(match.Groups["schema"].Value, "https", true) == 0 ? 443
                                : 80));
            this.Constructor(httpMethod, url, host, port, version);
        }
        */
        public HttpRequestHeader(string httpMethod, string host, int port, string version = "HTTP/1.1")
        {
            this.Constructor(httpMethod, host + ":" + port, host, port, version);
        }

        public HttpRequestHeader(string httpMethod, string url, string host, int port, string version = "HTTP/1.1")
        {
            this.Constructor(httpMethod, url, host, port, version);
        }

        private void Constructor(string httpMethod, string url, string host, int port, string version)
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
