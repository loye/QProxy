using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Q.Configuration;
using Q.Net;

namespace Q.Proxy
{
    public class SocksRepeater : Repeater
    {
        public SocksRepeater(listener listenerConfig)
            : base(listenerConfig)
        {
        }

        public override void Relay(Stream localStream)
        {
            using (Stream remoteStream = Connect(ref localStream))
            {
                var localTask = Task.Run(() =>
                {
                    Transfer(localStream, remoteStream);
                });
                var remoteTask = Task.Run(() =>
                {
                    Transfer(remoteStream, localStream);
                });

                Task.WaitAll(remoteTask, localTask);
            }
        }

        private void Transfer(Stream src, Stream dest)
        {
            byte[] buffer = new byte[BUFFER_LENGTH];
            for (int len = src.Read(buffer, 0, buffer.Length); len > 0; len = src.Read(buffer, 0, buffer.Length))
            {
                dest.Write(buffer, 0, len);
            }
        }

        private Stream Connect(ref Stream localStream)
        {
            string host;
            int port;
            IPAddress ip;
            SocksConnector.Instance.ConnectAsServer(localStream, out host, out port, out ip);
            return GetStream(host ?? ip.ToString(), port);
        }
    }
}
