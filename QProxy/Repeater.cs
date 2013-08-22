using System.IO;
using System.Net;
using System.Threading.Tasks;
using Q.Net;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        protected const int BUFFER_LENGTH = 4096;

        public IPEndPoint Proxy { get; set; }

        public Repeater()
        {
        }

        public abstract void Relay(Stream localStream);

        protected Stream GetRemoteStream(string host, int port)
        {
            //IPEndPoint endPoint = this.Proxy ?? new IPEndPoint(DnsHelper.GetHostAddress(requestHeader.Host), requestHeader.Port);
            //Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //socket.Connect(endPoint);
            //return new NetworkStream(socket, true);

            return new HttpTunnelStream(
                "http://localhost:1008/tunnel",
                //"https://tunnel.apphb.com/tunnel",
                host,
                port,
                true);
        }
    }
}
