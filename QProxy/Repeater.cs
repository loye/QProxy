using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        public IPEndPoint Proxy { get; set; }

        public Repeater()
        {
            
        }

        public abstract void Relay(Stream localStream);

    }
}
