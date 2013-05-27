using System;

namespace Q.Net
{
    public class HttpContent
    {
        private byte[] m_binary;

        private int m_startIndex;

        private int m_length;

        private int m_chunkedNextBlockOffset;

        public int Length { get { return m_length; } }

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

        public HttpContent Refresh(byte[] content, int startIndex, int length)
        {
            m_chunkedNextBlockOffset = m_startIndex == startIndex ? m_chunkedNextBlockOffset : startIndex;
            m_binary = content;
            m_startIndex = startIndex;
            m_length = length >= 0 || m_binary == null ? length : m_binary.Length - startIndex;
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
            byte[] result;
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
            int totalLength = m_startIndex + m_length, startIndex = m_chunkedNextBlockOffset;
            if (startIndex > totalLength - 5)
            {
                return false;
            }
            int contentLength = 0, i = startIndex;
            for (int temp = bin[i]; temp != 0x0D && i < totalLength; temp = bin[++i])
            {
                if (i >= totalLength - 1)
                {
                    return false;
                }
                contentLength = contentLength * 16 + (temp > 0x40 ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9 : temp - 0x30);
            }
            m_chunkedNextBlockOffset = i + contentLength + 4; // i + \r\n + contentLength + \r\n
            return contentLength == 0 || ValidateChunkedBlock();
        }
    }
}
