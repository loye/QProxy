using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

namespace Q.Net.Web
{
    public static class HttpTunnelNode
    {
        private static ConcurrentDictionary<string, Tunnel> tunnelPool = new ConcurrentDictionary<string, Tunnel>();

        public static void Connect(string id, string host, IPEndPoint endPoint)
        {
            tunnelPool.TryAdd(id, new Tunnel(id, host, endPoint));
        }

        public static void Write(string id, byte[] buffer, int offset, int count)
        {
            tunnelPool[id].Stream.Write(buffer, offset, count);
        }

        public static int Read(string id, byte[] buffer, int offset, int count)
        {
            return tunnelPool[id].Stream.Read(buffer, offset, count);
        }

        public static void Close(string id)
        {
            Tunnel tunnel;
            if (tunnelPool.TryRemove(id, out tunnel))
            {
                tunnel.Dispose();
            }
        }

        public static void Clear()
        {
            tunnelPool.Clear();
        }

        public static string GetDebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            sb.AppendLine("Tunnel Pool List:");
            foreach (var item in tunnelPool)
            {
                count++;
                sb.AppendLine(item.Value.ToString());
            }

            return String.Format("Tunnel Pool Count: {0}\r\n{1}", count, count > 0 ? sb.ToString() : null);
        }

        private class Tunnel : IDisposable
        {
            public string ID { get; set; }

            public string Host { get; set; }

            public IPEndPoint IPEndPoint { get; set; }

            public NetworkStream Stream { get; set; }

            private Socket m_socket;

            public Tunnel(string id, string host, IPEndPoint endPoint)
            {
                this.ID = id;
                this.Host = host;
                this.IPEndPoint = endPoint;
                this.m_socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.m_socket.Connect(endPoint);
                this.Stream = new NetworkStream(this.m_socket, true);
            }

            public override string ToString()
            {
                return String.Format("{{ ID: {0}, Host: {1}, IPEndPoint: {2} }}", this.ID, this.Host, this.IPEndPoint);
            }

            public void Dispose()
            {
                this.Stream.Dispose();
            }
        }
    }
}