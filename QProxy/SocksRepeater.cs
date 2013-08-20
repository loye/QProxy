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
        private byte[] m_InitResponse; // |VER|METHOD|

        private byte[] m_ConnectionResponse; // |VER|REP|RSV|ATYP|BND.ADDR|BIND.PORT|

        public SocksRepeater(IPEndPoint localEndPoint)
        {
            CreateResponse(localEndPoint);
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

                    Task.WaitAny(remoteTask, localTask);
                }
            }
        }

        private void Transfer(Stream src, Stream dest)
        {
            byte[] buf = new byte[40960];
            for (int len = src.Read(buf, 0, buf.Length); len > 0; len = src.Read(buf, 0, buf.Length))
            {
                dest.Write(buf, 0, len);
            }
        }

        private void CreateResponse(IPEndPoint localEndPoint)
        {
            m_InitResponse = new byte[] { 0x05, 0x00 };

            byte[] host = ASCIIEncoding.ASCII.GetBytes(localEndPoint.Address.ToString());
            int port = localEndPoint.Port;
            m_ConnectionResponse = new byte[host.Length + 7];
            m_ConnectionResponse[0] = 5;
            m_ConnectionResponse[1] = 0;
            m_ConnectionResponse[2] = 0;
            m_ConnectionResponse[3] = 3;
            m_ConnectionResponse[4] = (byte)host.Length;
            Array.Copy(host, 0, m_ConnectionResponse, 5, host.Length);
            m_ConnectionResponse[host.Length + 5] = (byte)(port / 256);
            m_ConnectionResponse[host.Length + 6] = (byte)(port % 256);
        }

        private Stream Connect(Stream localStream)
        {
            Stream remoteStream = null;
            byte[] buffer = new byte[128];
            int len = localStream.Read(buffer, 0, buffer.Length);
            InitRequest initRequest = InitRequest.Parse(buffer);
            if (initRequest.Version == 5 && initRequest.Methods.Contains((byte)0))
            {
                localStream.Write(m_InitResponse, 0, m_InitResponse.Length);
                len = localStream.Read(buffer, 0, buffer.Length);
                ConnectRequest connectRequest = ConnectRequest.Parse(buffer);
                remoteStream = GetRemoteStream(connectRequest);
                localStream.Write(m_ConnectionResponse, 0, m_ConnectionResponse.Length);
            }
            return remoteStream;
        }

        private Stream GetRemoteStream(ConnectRequest connectRequest)
        {
            Stream remoteStream = null;

            //IPEndPoint endPoint = new IPEndPoint(connectRequest.AddressType == 1 ? connectRequest.IPAddress : DnsHelper.GetHostAddress(connectRequest.Host), connectRequest.Port);
            //Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //socket.Connect(endPoint);
            //remoteStream = new NetworkStream(socket, true);

            remoteStream = new HttpTunnelStream(
                //"http://localhost:1008/b",
                "https://tunnel.apphb.com/tunnel",
                connectRequest.AddressType == 1 ? connectRequest.IPAddress.ToString() : connectRequest.Host,
                connectRequest.Port
                , null);// new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));

            return remoteStream;
        }

        /// <summary>
        /// |VER|NMETHODS|METHODS|
        /// </summary>
        private struct InitRequest
        {
            public byte Version;
            public byte[] Methods;

            public static InitRequest Parse(byte[] source)
            {
                InitRequest request = new InitRequest();
                request.Version = source[0];
                request.Methods = new byte[source[1]];
                Array.Copy(source, 2, request.Methods, 0, source[1]);
                return request;
            }
        }

        /// <summary>
        /// |VER|CMD|RSV|ATYP|DST.ADDR|DST.PORT|
        /// </summary>
        private struct ConnectRequest
        {
            public byte Version;
            public byte Command;
            public byte AddressType;
            public IPAddress IPAddress;
            public string Host;
            public int Port;

            public static ConnectRequest Parse(byte[] source)
            {
                ConnectRequest request = new ConnectRequest();
                request.Version = source[0];
                request.Command = source[1];
                request.AddressType = source[3];
                switch (request.AddressType)
                {
                    case 1:
                        request.IPAddress = new IPAddress(new byte[4] { source[4], source[5], source[6], source[7] });
                        request.Port = source[8] * 256 + source[8];
                        break;
                    case 3:
                        request.Host = ASCIIEncoding.ASCII.GetString(source, 5, source[4]);
                        request.Port = source[5 + source[4]] * 256 + source[5 + source[4] + 1];
                        break;
                    case 4:
                        throw new NotSupportedException("IPv6 not supported yet!");
                    default:
                        throw new Exception("AddressType incorrect!");
                }

                return request;
            }
        }
    }
}
