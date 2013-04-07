using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QProxy
{
    public sealed class HttpHeaderKey
    {
        public const string Host = "Host";
        public const string Content_Length = "Content-Length";
        public const string Transfer_Encoding = "Transfer-Encoding";
        public const string Connection = "Connection";

        public const string SCV_Id = "SCV-Id";
        public const string SCV_Host = "SCV-Host";
        public const string SCV_Port = "SCV-Port";
        public const string SCV_IP = "SCV-IP";
        public const string SCV_SSL = "SCV-SSL";
        public const string SCV_Encrypted = "SCV-Encrypted";
        public const string SCV_Exception = "SCV-Exception";
    }

    public sealed class HttpMethod
    {
        public const string Connect = "CONNECT";
        public const string POST = "POST";
    }

}
