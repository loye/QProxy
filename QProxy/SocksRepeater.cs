using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Q.Net;
using System.Diagnostics;

namespace Q.Proxy
{
    public class SocksRepeater : Repeater
    {
        public SocksRepeater()
        {
        }

        public override void Relay(Stream localStream)
        {
            using (Stream remoteStream = Connect(localStream))
            {
                if (remoteStream != null)
                {
                    var remoteTask = Task.Run(() =>
                    {
                        Transfer(remoteStream, localStream);
                    });
                    var localTask = Task.Run(() =>
                    {
                        Transfer(localStream, remoteStream);
                    });

                    Task.WaitAll(remoteTask, localTask);
                }
            }
        }

        private void Transfer(Stream src, Stream dest)
        {
            byte[] buf = new byte[4096];
            for (int len = src.Read(buf, 0, buf.Length); len > 0; len = src.Read(buf, 0, buf.Length))
            {
                dest.Write(buf, 0, len);
            }
        }

        private Stream Connect(Stream localStream)
        {
            string host;
            int port;
            IPAddress ip;
            SocksConnector.Instance.ConnectAsServer(localStream, out host, out port, out ip);
            return GetRemoteStream(host ?? ip.ToString(), port, ip);
        }

        private Stream GetRemoteStream(string host, int port, IPAddress ip = null)
        {
            Stream remoteStream = null;

            IPEndPoint endPoint = new IPEndPoint(ip != null ? ip : DnsHelper.GetHostAddress(host), port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            remoteStream = new NetworkStream(socket, true);

            //remoteStream = new HttpTunnelStream(
            //    //"http://localhost:1008/tunnel",
            //    "https://tunnel.apphb.com/tunnel",
            //    host,
            //    port
            //    , null);// new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));

            return remoteStream;
        }
    }
}
