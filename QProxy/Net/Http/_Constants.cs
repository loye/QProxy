using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Net
{
    public sealed class HttpHeaderKey
    {
        public const string Host = "Host";
        public const string Content_Length = "Content-Length";
        public const string Transfer_Encoding = "Transfer-Encoding";
        public const string Connection = "Connection";
        public const string Proxy_Connection = "Proxy-Connection";
    }

    public sealed class HttpHeaderValue
    {
        public sealed class Transfer_Encoding
        {
            public const string Chunked = "chunked";
        }

        public sealed class Connection
        {
            public const string Close = "close";
            public const string Keep_Alive = "keep-alive";
        }
    }

    public sealed class HttpMethod
    {
        public const string Connect = "CONNECT";
        public const string POST = "POST";
    }

    public sealed class HttpStatus
    {
        public const string Connection_Established = "Connection Established";
    }

    public sealed class HttpVersion
    {
        public const string Default = "HTTP/1.1";
    }
}
