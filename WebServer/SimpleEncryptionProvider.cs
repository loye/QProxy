using System;

namespace Q
{
    public class SimpleEncryptionProvider
    {
        private byte[] seed;

        public SimpleEncryptionProvider(string key = null)
        {
            seed = new byte[16];
            char[] ka = String.IsNullOrEmpty(key) ? "!1@2#3$4%5^6&7*8".ToCharArray() : key.ToCharArray();
            int kl = ka.Length - 1;
            for (int i = 0; i < 16; i++)
            {
                seed[i] = (byte)((ka[i & kl] + kl) & 85);
            }
        }

        public void Encrypt(byte[] src, int offset, int count, int globalOffset = 0)
        {
            for (int i = offset, os = globalOffset % 16; i < count + offset; i++, os++)
            {
                int steps = (os & 7) + ((os & 8) == 0 ? -8 : 1);
                src[i] = (byte)~((src[i] + steps * seed[os & 15]) & 255);
            }
        }

        public void Decrypt(byte[] src, int offset, int count, int globalOffset = 0)
        {
            for (int i = offset, os = globalOffset % 16; i < count + offset; i++, os++)
            {
                int steps = (os & 7) + ((os & 8) == 0 ? -8 : 1);
                src[i] = (byte)((~src[i] - steps * seed[os & 15]) & 255);
            }
        }
    }
}
