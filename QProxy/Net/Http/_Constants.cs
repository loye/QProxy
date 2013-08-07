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

    public sealed class HttpHeaderCustomKey
    {
        private const string CustomPrefix = "Q-";

        public const string Type = CustomPrefix + "Type";
        public const string Host = CustomPrefix + "Host";
        public const string Port = CustomPrefix + "Port";
        public const string IP = CustomPrefix + "IP";
        public const string SSL = CustomPrefix + "SSL";
        public const string Encrypted = CustomPrefix + "Encrypted";
        public const string Exception = CustomPrefix + "Exception";
    }

}
