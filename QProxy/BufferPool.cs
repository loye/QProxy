using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Proxy
{
    public class BufferPool : IDisposable
    {
        private MemoryStream m_buffer;

        public int ReadPosition { get; set; }

        //public int WritePosition { get { return (int)m_buffer.Position; } set { m_buffer.Position = value; } }

        public int Length { get { return (int)m_buffer.Length; } }

        public BufferPool()
        {
            m_buffer = new MemoryStream();
            this.ReadPosition = 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int len = 0;
            int totalLength = this.Length;
            if (totalLength > 0)
            {
                byte[] bin = m_buffer.GetBuffer();
                len = Math.Min(count, totalLength);
                Array.Copy(bin, buffer, len);
                this.ReadPosition += len;
            }
            return len;
        }

        public byte[] ReadAllBytes()
        {
            int totalLength = this.Length;
            byte[] buffer = new byte[totalLength];
            if (totalLength > 0)
            {
                byte[] bin = m_buffer.GetBuffer();
                Array.Copy(bin, buffer, totalLength);
                this.ReadPosition += totalLength;
            }
            return buffer;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            m_buffer.Write(buffer, offset, count);
        }

        public byte[] GetBuffer()
        {
            return m_buffer.GetBuffer();
        }

        public void Dispose()
        {
            m_buffer.Dispose();
        }
    }
}
