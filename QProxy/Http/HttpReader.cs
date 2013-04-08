using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Http
{
    public class HttpReader
    {
        private const int BUFFER_LENGTH = 1024;

        public event Action<HttpHeader, Stream> OnHeaderReady;

        public HttpPackage Read(Stream stream)
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
                    if (ValidatePackage(mem, ref package))
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

        private bool ValidatePackage(MemoryStream memoryStream, ref HttpPackage package)
        {
            byte[] source = memoryStream.GetBuffer();
            int length = (int)memoryStream.Length;
            if (package == null)
            {
                HttpHeader header;
                if (HttpHeader.TryParse(source, 0, length, out header))
                {
                    this.OnHeaderReady(header, memoryStream);
                    int headerLength = header.Length;
                    package = new HttpPackage(header, new HttpContent(source, headerLength, length - headerLength));
                }
            }
            return package != null && package.HttpContent.Append(length - 0).Validate(package.HttpHeader.ContentLength);
        }
    }
}
