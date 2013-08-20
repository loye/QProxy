using Q.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Q.Proxy
{
    public class Listener
    {
        private Socket m_ListenSocket;

        private Repeater m_repeater;

        public IPEndPoint Proxy { get; private set; }

        #region Constructors

        public Listener(string ip, int port, bool decryptSSL = false)
            : this(new IPEndPoint(IPAddress.Parse(ip), port), decryptSSL)
        { }

        public Listener(string ip, int port, string proxyIP, int proxyPort, bool decryptSSL = false)
            : this(new IPEndPoint(IPAddress.Parse(ip), port), new IPEndPoint(IPAddress.Parse(proxyIP), proxyPort), decryptSSL)
        { }

        public Listener(IPEndPoint endPoint, bool decryptSSL = false) :
            this(endPoint, null, decryptSSL)
        { }

        public Listener(IPEndPoint endPoint, IPEndPoint proxy, bool decryptSSL = false)
        {
            this.Proxy = proxy;
            this.m_ListenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.m_ListenSocket.Bind(endPoint);
            m_repeater = new SocksRepeater((IPEndPoint)(endPoint)); //new HttpRepeater(proxy, decryptSSL);
        }

        #endregion

        public Listener Start()
        {
            this.m_ListenSocket.Listen(500);
            ThreadPool.SetMinThreads(1000, 1000);

            Task.Run(new Action(Accept));
            Task.Run(new Action(Accept));

            Logger.Info(this.ToString());
            return this;
        }

        public Listener Stop()
        {
            this.m_ListenSocket.Shutdown(SocketShutdown.Both);
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine("----------------------------------------------------------------------")
                .AppendFormat("Listen Address\t:\t{0}\n", this.m_ListenSocket.LocalEndPoint)
                .AppendFormat("Repeater Type\t:\t{0}\n", m_repeater);
            if (this.Proxy != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.Proxy);
            }
            sb.AppendLine("----------------------------------------------------------------------");
            return sb.ToString();
        }

        #region Private Methods

        private void Accept()
        {
            while (true)
            {
                var clientSocket = this.m_ListenSocket.Accept();
                Task.Run(() =>
                {
                    Relay(clientSocket);
                });
            }
        }

        private void Relay(Socket clientSocket)
        {
            using (Stream netStream = new NetworkStream(clientSocket, true))
            {
                try
                {
                    m_repeater.Relay(netStream);
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    Logger.PublishException(ex);
                }
            }
        }

        #endregion
    }
}
