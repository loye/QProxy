using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Net
{
    public sealed class HttpHeaderCustomKey
    {
        private const string CustomPrefix = "Q-";

        public const string ID = CustomPrefix + "ID";
        public const string Action = CustomPrefix + "Action";
        public const string Host = CustomPrefix + "Host";
        public const string Port = CustomPrefix + "Port";
        public const string IP = CustomPrefix + "IP";
        public const string Length = CustomPrefix + "Length";
        public const string Message = CustomPrefix + "Message";
        public const string Encrypted = CustomPrefix + "Encrypted";
        public const string Exception = CustomPrefix + "Exception";
    }

    public sealed class HttpHeaderCustomValue
    {
        public sealed class Action
        {
            public const string Connect = "connect";
            public const string Write = "write";
            public const string Read = "read";
            public const string Close = "close";
            public const string Debug = "debug";
        }
    }
}
