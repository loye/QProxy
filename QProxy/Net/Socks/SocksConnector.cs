using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Q.Net
{
    /// <summary>
    /// Socks4:
    ///     |VER{1}4|ATYP{1}1|DST.PORT{2}|DST.ADDR{4}|USERID{}|END{1}0|[?(Socks4a)DST.ADDR=0,0,0,1?DST.HOST{}|END{1}0]
    ///     |REP{1}0|PROTOCOL{1}90|DST.PORT{2}|DST.ADDR{4}|
    /// Socks5:
    ///     |VER{1}5|NMETHODS{1}|METHODS{NMETHODS}|
    ///     |VER{1}5|METHOD{1}|
    ///     |VER{1}5|CMD{1}[1(TCP)|3(UDP)]|RSV{1}0|ATYP{1}[1(IPv4)/3(HOST)/4(IPv6)]|[DST.ADDR{4}/DST.NHOST{1}|DST.HOST{DST.NHOST}]|DST.PORT{2}|
    ///     |VER{1}5|REP{1}0|RSV{1}0|ATYP{1}1|BND.ADDR{4}|BIND.PORT{2}| : 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 
    /// </summary>
    public class SocksConnector
    {
        private static SocksConnector m_instance;

        private static object locker = new object();

        private SocksConnector() { }

        public static SocksConnector Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new SocksConnector();
                        }
                    }
                }
                return m_instance;
            }
        }

        #region Client Socks5

        public Stream ConnectAsClientV5(IPEndPoint serverEndPoint, IPAddress ip, int port)
        {
            Socket socket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverEndPoint);
            var remoteStream = new NetworkStream(socket, true);
            return this.ConnectAsClientV5(remoteStream, ip, port);
        }

        public Stream ConnectAsClientV5(IPEndPoint serverEndPoint, string host, int port)
        {
            Socket socket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverEndPoint);
            var remoteStream = new NetworkStream(socket, true);
            return this.ConnectAsClientV5(remoteStream, host, port);
        }

        public Stream ConnectAsClientV5(Stream serverStream, IPAddress ip, int port)
        {
            byte[] buffer = new byte[16];
            byte[] ipBin = ip.GetAddressBytes();
            serverStream.Write(new byte[] { 5, 1, 0 }, 0, 3);
            var len = serverStream.Read(buffer, 0, buffer.Length);
            byte[] bin = new byte[] { 5, 1, 0, 1, ipBin[0], ipBin[1], ipBin[2], ipBin[3], (byte)(port / 256), (byte)(port % 256) };
            serverStream.Write(bin, 0, bin.Length);
            len = serverStream.Read(buffer, 0, 10);
            return serverStream;
        }

        public Stream ConnectAsClientV5(Stream serverStream, string host, int port)
        {
            byte[] buffer = new byte[16];
            byte[] hostBin = ASCIIEncoding.ASCII.GetBytes(host);
            serverStream.Write(new byte[] { 5, 1, 0 }, 0, 3);
            var len = serverStream.Read(buffer, 0, buffer.Length);
            byte[] bin = new byte[hostBin.Length + 7];
            bin[0] = 5;
            bin[1] = 1;
            bin[2] = 0;
            bin[3] = 3;
            bin[4] = (byte)hostBin.Length;
            Array.Copy(hostBin, 0, bin, 5, hostBin.Length);
            bin[hostBin.Length + 5] = (byte)(port / 256);
            bin[hostBin.Length + 6] = (byte)(port % 256);
            serverStream.Write(bin, 0, bin.Length);
            len = serverStream.Read(buffer, 0, 10);
            return serverStream;
        }

        #endregion


        #region Client Socks4

        public Stream ConnectAsClientV4(IPEndPoint serverEndPoint, IPAddress ip, int port)
        {
            Socket socket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverEndPoint);
            var remoteStream = new NetworkStream(socket, true);
            return this.ConnectAsClientV4(remoteStream, ip, port);
        }

        public Stream ConnectAsClientV4(Stream serverStream, IPAddress ip, int port)
        {
            byte[] buffer = new byte[8];
            byte[] ipBin = ip.GetAddressBytes();
            byte[] bin = new byte[] { 4, 1, (byte)(port / 256), (byte)(port % 256), ipBin[0], ipBin[1], ipBin[2], ipBin[3], 0 };
            serverStream.Write(bin, 0, bin.Length);
            var len = serverStream.Read(buffer, 0, 8);
            return serverStream;
        }

        #endregion

        #region Server

        public Stream ConnectAsServer(Stream clientStream, out string host, out int port, out IPAddress ip)
        {
            host = null;
            port = 0;
            ip = null;
            byte[] buffer = new byte[264];
            int len = clientStream.Read(buffer, 0, buffer.Length);
            int version = buffer[0];
            switch (version)
            {
                case 4:
                    {
                        ip = new IPAddress(new byte[4] { buffer[4], buffer[5], buffer[6], buffer[7] });
                        port = buffer[2] * 256 + buffer[3];
                        // userid                        
                        string userid = null;
                        int index = 8;
                        for (index = 8; index < len; index++)
                        {
                            if (buffer[index] == 0)
                            {
                                userid = ASCIIEncoding.ASCII.GetString(buffer, 8, index - 8);
                                break;
                            }
                        }
                        // host (Socks4a)
                        if (ip.Equals(new IPAddress(new byte[] { 0, 0, 0, 1 })))
                        {
                            for (int i = ++index; i < len; i++)
                            {
                                if (buffer[i] == 0)
                                {
                                    host = ASCIIEncoding.ASCII.GetString(buffer, index, i - index);
                                    ip = null;
                                    break;
                                }
                            }
                        }
                        clientStream.Write(new byte[] { 0, 90, buffer[2], buffer[3], buffer[4], buffer[5], buffer[6], buffer[7] }, 0, 8);
                    }
                    break;
                case 5:
                    {
                        byte[] methods = new byte[buffer[1]];
                        Array.Copy(buffer, 2, methods, 0, Math.Min(buffer[1], len - 2));
                        if (methods.Contains((byte)0))
                        {
                            clientStream.Write(new byte[] { 5, 0 }, 0, 2);
                            len = clientStream.Read(buffer, 0, buffer.Length);
                            int addressType = buffer[3];
                            switch (addressType)
                            {
                                case 1:
                                    ip = new IPAddress(new byte[4] { buffer[4], buffer[5], buffer[6], buffer[7] });
                                    port = buffer[8] * 256 + buffer[8];
                                    break;
                                case 3:
                                    host = ASCIIEncoding.ASCII.GetString(buffer, 5, buffer[4]);
                                    port = buffer[5 + buffer[4]] * 256 + buffer[5 + buffer[4] + 1];
                                    break;
                                case 4:
                                    throw new NotSupportedException("IPv6 not supported yet!");
                                default:
                                    throw new Exception("AddressType incorrect!");
                            }
                            clientStream.Write(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, 0, 10);
                        }
                        else
                        {
                            throw new NotSupportedException(String.Format("Anonymous authentication supported only!"));
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException(String.Format("Socks version {0} not supported!", version));
            }
            return clientStream;
        }

        #endregion
    }
}
