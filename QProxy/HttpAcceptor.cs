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

        public bool TryAccept(Stream stream, out BufferPool bufferPool, out HttpHeader httpHeader)
        {
            httpHeader = null;
            byte[] buffer = new byte[BUFFER_LENGTH];
            bufferPool = new BufferPool();
            for (int len = stream.Read(buffer, 0, buffer.Length);
                len > 0;
                len = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
            {
                bufferPool.Write(buffer, 0, len);
                byte[] bin = bufferPool.GetBuffer();
                int curLenght = bufferPool.Length;
                if (HttpHeader.TryParse(bin, 0, curLenght, out httpHeader))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
