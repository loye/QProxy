using System;
using System.IO;

namespace Q.Net
{
    public class HttpPackage
    {
        public const int BUFFER_LENGTH = 4096;

        public HttpHeader HttpHeader { get; private set; }

        public HttpContent HttpContent { get; private set; }

        public bool IsValid { get; private set; }

        public bool IsCompleted
        {
            get
            {
                if (this.IsValid)
                {
                    if (this.HttpHeader is Net.HttpRequestHeader
                        && (this.HttpHeader as Net.HttpRequestHeader).HttpMethod == HttpMethod.Connect)
                    {
                        return true;
                    }
                    if (this.HttpHeader.ContentLength == 0
                         && String.Compare(this.HttpHeader[HttpHeaderKey.Connection], HttpHeaderValue.Connection.Close, true) == 0)
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

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

        public static HttpPackage Parse(Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                return null;
            }
            HttpPackage package = null;
            byte[] buffer = new byte[BUFFER_LENGTH];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = stream.Read(buffer, 0, buffer.Length);
                    len > 0;
                    len = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    if (ValidatePackage(bin, 0, (int)mem.Length, ref package))
                    {
                        if (package.HttpHeader.ContentLength == 0
                            && String.Compare(package.HttpHeader[HttpHeaderKey.Connection], HttpHeaderValue.Connection.Close, true) == 0
                            && !package.HttpHeader.StartLine.Contains(HttpStatus.Connection_Established)) // Connection: close
                        {
                            continue;
                        }
                        break;
                    }
                }
            }
            return package;
        }

        public static bool ValidatePackage(byte[] source, int startIndex, int length, ref HttpPackage package)
        {
            bool isValid = false;
            if (package == null)
            {
                HttpHeader header;
                if (HttpHeader.TryParse(source, startIndex, length, out header))
                {
                    package = new HttpPackage(header, new HttpContent(source, startIndex + header.Length, length - header.Length));
                }
            }
            if (package != null)
            {
                isValid = package.HttpContent
                    .Refresh(source, startIndex + package.HttpHeader.Length, length - package.HttpHeader.Length)
                    .Validate(package.HttpHeader.ContentLength);
                package.IsValid = isValid;
            }
            return isValid;
        }

    }
}
