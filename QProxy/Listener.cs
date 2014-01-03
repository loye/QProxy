using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Q.Proxy.Debug;

namespace Q.Proxy
{
    public class Listener
    {
        private Socket m_ListenSocket;

        private Repeater m_repeater;

        public bool Started { get; private set; }

        public Listener(string ip, int port, Repeater repeater)
            : this(new IPEndPoint(IPAddress.Parse(ip), port), repeater)
        { }

        public Listener(IPEndPoint endPoint, Repeater repeater)
        {
            this.m_ListenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.m_ListenSocket.Bind(endPoint);
            m_repeater = repeater;
            ThreadPool.SetMinThreads(1000, 1000);
        }

        public Listener Start()
        {
            this.m_ListenSocket.Listen(500);

            Task.Run(new Action(Accept));
            Task.Run(new Action(Accept));
            this.Started = true;

            Logger.Current.Info(this.ToString());
            Logger.Current.Info("Start listening");
            return this;
        }

        public Listener Stop()
        {
            this.Started = false;
            this.m_ListenSocket.Close();
            Logger.Current.Info("Stopped listening");
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Listen Address\t: {0}\r\n", this.m_ListenSocket.LocalEndPoint);
            sb.AppendFormat("Repeater\t: {0}\r\n", m_repeater);
            return sb.ToString();
        }

        private void Accept()
        {
            while (this.Started)
            {
                Socket clientSocket;
                try
                {
                    clientSocket = this.m_ListenSocket.Accept();
                }
                catch (Exception ex)
                {
                    if (this.Started)
                    {
                        Logger.Current.PublishException(ex);
                    }
                    break;
                }
                Task.Run(() =>
                {
                    Logger.Current.Debug(String.Format("Connected: {0}", clientSocket.LocalEndPoint));
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
                            Logger.Current.PublishException(ex);
                        }
                    }
                });
            }
        }
    }
}
