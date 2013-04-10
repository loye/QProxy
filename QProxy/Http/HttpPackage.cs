using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Q.Http
{
    public class HttpPackage
    {
        private const int BUFFER_LENGTH = 1024;

        public HttpHeader HttpHeader { get; private set; }

        public HttpContent HttpContent { get; private set; }

        public HttpPackage(HttpHeader httpHeader, HttpContent httpContent)
        {
            this.HttpHeader = httpHeader;
            this.HttpContent = httpContent;
        }

        public static HttpPackage Parse(Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                return null;
            }
            HttpPackage package = null;
            int bufferLength = BUFFER_LENGTH;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = stream.Read(buffer, 0, buffer.Length);
                    len > 0;
                    len = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    if (ValidatePackage(bin, (int)mem.Length, ref package))
                    {
                        if (package.HttpHeader.ContentLength == 0
                            && String.Compare(package.HttpHeader[HttpHeaderKey.Connection], "close", true) == 0
                            && !package.HttpHeader.StartLine.Contains("Connection Established")) // Connection: close
                        {
                            continue;
                        }
                        break;
                    }
                }
            }
            return package;
        }

        private static bool ValidatePackage(byte[] source, int length, ref HttpPackage package)
        {
            if (package == null)
            {
                HttpHeader header;
                if (HttpHeader.TryParse(source, 0, length, out header))
                {
                    int headerLength = header.Length;
                    package = new HttpPackage(header, new HttpContent(source, headerLength, length - headerLength));
                }
            }
            return package != null && package.HttpContent.Append(length - 0).Validate(package.HttpHeader.ContentLength);
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
