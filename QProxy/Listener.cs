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

        public bool DecryptSSL { get; private set; }

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
            this.DecryptSSL = decryptSSL;
            m_tcpListener = new TcpListener(endPoint);
            m_repeater = new Repeater(this.Proxy);
            m_repeater.DecryptSSL = this.DecryptSSL;
        }

        public Listener Start()
        {
            this.m_tcpListener.Start(50);
            Logger.Info(this.ToString());
            this.m_tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAccept), m_tcpListener);
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

        private void DoAccept(IAsyncResult ar)
        {
            TcpListener tcp = (ar.AsyncState as TcpListener);
            tcp.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcp);
            using (TcpClient client = tcp.EndAcceptTcpClient(ar))
            using (NetworkStream networkStream = client.GetStream())
            {
                Stream localStream = networkStream;
                try
                {
                    Console.WriteLine("request begin");
                    m_repeater.Relay(ref localStream);
                    Console.WriteLine("request end");
                }
                catch (Exception ex)
                {
                    //Logger.PublishException(ex, request != null ? String.Format("{0}:{1}\n{2}", request.Host, request.Port, request.StartLine) : null);
                }
                finally
                {
                    if (localStream != null)
                    {
                        localStream.Dispose();
                    }
                }
            }
        }
    }
}
