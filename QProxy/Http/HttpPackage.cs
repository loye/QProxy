using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Q.Http
{
    public class HttpPackage
    {
        public HttpHeader HttpHeader { get; private set; }

        public HttpContent HttpContent { get; private set; }

        public HttpPackage(HttpHeader httpHeader, HttpContent httpContent)
        {
            this.HttpHeader = httpHeader;
            this.HttpContent = httpContent;
        }

        public byte[] ToBinary()
        {
            byte[] headerBin = this.HttpHeader.ToBinary();
            byte[] contentBin = this.HttpContent.ToBinary();
            byte[] bin = new byte[headerBin.Length + contentBin.Length];
            Array.Copy(headerBin, bin, headerBin.Length);
            Array.Copy(contentBin, 0, bin, headerBin.Length, contentBin.Length);
            return bin;
        }
    }
}
