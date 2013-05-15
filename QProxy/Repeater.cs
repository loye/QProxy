using System.IO;
using System.Net;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        public IPEndPoint Proxy { get; set; }

        public Repeater(IPEndPoint proxy = null)
        {
            this.Proxy = proxy;
        }

        public abstract void Relay(Stream localStream);

    }
}
