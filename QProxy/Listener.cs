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

        public bool Started { get; private set; }

        public Listener(string ip, int port)
            : this(new IPEndPoint(IPAddress.Parse(ip), port))
        { }

        public Listener(IPEndPoint endPoint)
        {
            this.m_ListenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.m_ListenSocket.Bind(endPoint);
            //m_repeater = new HttpRepeater();
            m_repeater = new SocksRepeater();
            ThreadPool.SetMinThreads(1000, 1000);
        }

        public Listener Start()
        {
            this.m_ListenSocket.Listen(500);

            Task.Run(new Action(Accept));
            Task.Run(new Action(Accept));
            this.Started = true;

            Logger.Info(this.ToString());
            return this;
        }

        public Listener Stop()
        {
            throw new NotImplementedException();
            //this.Started = false;
            //return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("----------------------------------------------------------------------");
            sb.AppendFormat("Listen Address\t:\t{0}\n", this.m_ListenSocket.LocalEndPoint);
            sb.AppendFormat("Repeater Type\t:\t{0}\n", m_repeater);
            if (this.Proxy != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.Proxy);
            }
            sb.AppendLine("----------------------------------------------------------------------");
            return sb.ToString();
        }

        private void Accept()
        {
            while (this.Started)
            {
                var clientSocket = this.m_ListenSocket.Accept();
                Task.Run(() =>
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
                });
            }
        }
    }
}
