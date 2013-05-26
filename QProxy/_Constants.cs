using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Proxy
{
    public sealed class HttpHeaderCustomKey
    {
        private const string CustomPrefix = "Q-";

        public const string Id = CustomPrefix + "Id";
        public const string Host = CustomPrefix + "Host";
        public const string Port = CustomPrefix + "Port";
        public const string IP = CustomPrefix + "IP";
        public const string SSL = CustomPrefix + "SSL";
        public const string Encrypted = CustomPrefix + "Encrypted";
        public const string Exception = CustomPrefix + "Exception";
    }
}
