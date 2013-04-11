using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Q.Http;

namespace Q.Proxy
{
    public class HttpAcceptor
    {
        private const int BUFFER_LENGTH = 1024;

        public HttpHeader Accept(Stream stream, out byte[] buffer)
        {
            HttpHeader header = null;
            HttpRequestHeader requestHeader;
            buffer = new byte[BUFFER_LENGTH];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = stream.Read(buffer, 0, buffer.Length);
                    len > 0;
                    len = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    int length = (int)mem.Length;
                    if (HttpHeader.TryParse(bin, 0, length, out header))
                    {
                        requestHeader = header as HttpRequestHeader;
                        break;
                    }
                }
            }
            return header;
        }
    }
}
