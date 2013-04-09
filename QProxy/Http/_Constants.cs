using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Http
{
    public sealed class HttpHeaderKey
    {
        public const string Host = "Host";
        public const string Content_Length = "Content-Length";
        public const string Transfer_Encoding = "Transfer-Encoding";
        public const string Connection = "Connection";
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

}
