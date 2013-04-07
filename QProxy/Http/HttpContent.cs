using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QProxy
{
    public class HttpContent
    {
        private byte[] m_binary;

        private int m_startIndex;

        private int m_length;

        private int m_chunkedNextBlockOffset;

        public HttpContent(byte[] content, int startIndex, int length)
        {
            m_binary = content;
            m_startIndex = startIndex;
            m_length = length >= 0 || m_binary == null ? length : m_binary.Length - startIndex;
            m_chunkedNextBlockOffset = startIndex;
        }

        public static HttpContent Empty
        {
            get
            {
                return new HttpContent(null, 0, 0);
            }
        }

        public HttpContent Append(int length)
        {
            m_length = Math.Min(m_binary.Length - m_startIndex, length);
            return this;
        }

        public bool Validate(int expectLength)
        {
            bool result = false, isChunked = expectLength == -1;
            if (expectLength == 0)
            {
                result = true;
            }
            else if (m_binary != null)
            {
                result = isChunked ? ValidateChunkedBlock() : m_length >= expectLength;
            }
            return result;
        }

        public byte[] ToBinary()
        {
            byte[] result = null;
            if (m_binary != null && m_binary.Length >= m_startIndex + m_length)
            {
                result = new byte[m_length];
                Array.Copy(m_binary, m_startIndex, result, 0, m_length);
            }
            else
            {
                result = new byte[0];
            }
            return result;
        }

        private bool ValidateChunkedBlock()
        {
            byte[] bin = m_binary;
            int length = m_length, startIndex = m_chunkedNextBlockOffset;
            if (m_binary == null || length < 5)
            {
                return false;
            }
            int contentLength = 0, i = startIndex;
            for (int temp = bin[i]; temp != 0x0D && i < length; temp = bin[++i])
            {
                if (i >= length - 1)
                {
                    return false;
                }
                contentLength = contentLength * 16 + (temp > 0x40 ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9 : temp - 0x30);
            }
            m_chunkedNextBlockOffset = i + 2 + contentLength + 2; // i + \r\n + contentLength + \r\n
            return contentLength == 0 || ValidateChunkedBlock();
        }
    }
}
