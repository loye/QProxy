using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q
{
    public static class Extenstions
    {
        public static string ToHexString(this int source, bool upperCase = false)
        {
            StringBuilder sb = new StringBuilder();
            int letterBase = upperCase ? 0x40 : 0x60;
            for (int i = source; i > 0; i = i / 16)
            {
                int remainder = i % 16;
                if (remainder < 10)
                {
                    sb.Insert(0, (char)(remainder + 0x30));
                }
                else
                {
                    sb.Insert(0, (char)(remainder - 9 + letterBase));
                }
            }

            return sb.ToString();
        }

        public static byte[] ToChunked(this byte[] source)
        {
            return source.ToChunked(0, source.Length);
        }

        public static byte[] ToChunked(this byte[] source, int offset, int count)
        {
            byte[] begin = ASCIIEncoding.ASCII.GetBytes(count.ToHexString() + "\r\n");
            byte[] result = new byte[begin.Length + count + 2];
            Array.Copy(begin, 0, result, 0, begin.Length);
            Array.Copy(source, offset, result, begin.Length, count);
            result[result.Length - 2] = 13;
            result[result.Length - 1] = 10;
            return result;
        }
    }
}
