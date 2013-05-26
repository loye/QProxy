using Q.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Proxy
{
    public class Listener
    {
        private TcpListener m_tcpListener;

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
            m_tcpListener = new TcpListener(endPoint);
            m_repeater = new HttpRepeater(proxy, decryptSSL);
        }

        #endregion

        public Listener Start()
        {
            this.m_tcpListener.Start(50);
            Logger.Info(this.ToString());
            Accept(this.m_tcpListener);
            return this;
        }

        public Listener Stop()
        {
            this.m_tcpListener.Stop();
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine("----------------------------------------------------------------------")
                .AppendFormat("Listen Address\t:\t{0}\n", this.m_tcpListener.LocalEndpoint)
                .AppendFormat("Repeater Type\t:\t{0}\n", m_repeater);
            if (this.Proxy != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.Proxy);
            }
            sb.AppendLine("----------------------------------------------------------------------");
            return sb.ToString();
        }

        private void Accept(TcpListener listener)
        {
            listener.AcceptTcpClientAsync().ContinueWith(async (clientAsync) =>
            {
                Accept(listener);

                using (TcpClient client = await clientAsync)
                using (NetworkStream networkStream = client.GetStream())
                {
                    try
                    {
                        m_repeater.Relay(networkStream);
                    }
                    catch (Exception ex)
                    {
                        Logger.PublishException(ex);
                    }
                }
            });
        }
    }
}
