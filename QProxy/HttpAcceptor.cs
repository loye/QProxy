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

        public HttpHeader Accept(Stream stream, out byte[] recievedBytes, out int length)
        {
            recievedBytes = null;
            length = 0;
            HttpHeader header = null;
            HttpRequestHeader requestHeader;
            byte[] buffer = new byte[BUFFER_LENGTH];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int l = stream.Read(buffer, 0, buffer.Length);
                    l > 0;
                    l = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
                {
                    mem.Write(buffer, 0, l);
                    byte[] bin = mem.GetBuffer();
                    int len = (int)mem.Length;
                    if (HttpHeader.TryParse(bin, 0, len, out header))
                    {
                        requestHeader = header as HttpRequestHeader;
                        recievedBytes = bin;
                        length = len;
                        break;
                    }
                }
            }
            return header;
        }
    }
}
