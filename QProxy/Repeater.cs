using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Q.Configuration;
using Q.Net;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        protected const int BUFFER_LENGTH = 4096;

        private tunnel m_tunnel;

        public IPEndPoint Proxy { get; set; }

        public Repeater(listener listenerConfig)
        {
            m_tunnel = listenerConfig.tunnel;
        }

        public abstract void Relay(Stream localStream);

        protected Stream GetStream(string host, int port)
        {
            switch (m_tunnel.type)
            {
                case tunnelType.local:
                    return GetLocalStream(host, port);
                case tunnelType.http:
                    return GetHttpTunnelStream(host, port);
                default:
                    return GetLocalStream(host, port);
            }
        }

        private Stream GetLocalStream(string host, int port)
        {
            IPEndPoint endPoint = this.Proxy ?? new IPEndPoint(DnsHelper.GetHostAddress(host), port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            return new NetworkStream(socket, true);
        }

        private Stream GetHttpTunnelStream(string host, int port)
        {
            return new HttpTunnelStream(
                m_tunnel.url,
                host,
                port,
                m_tunnel.encrypted);
        }
    }
}
