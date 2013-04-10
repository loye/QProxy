using Q;
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


        public IPEndPoint Proxy { get; set; }

        public Listener(string ip, int port)
            : this(new IPEndPoint(IPAddress.Parse(ip), port))
        { }

        public Listener(string ip, int port, string proxyIP, int proxyPort)
            : this(new IPEndPoint(IPAddress.Parse(ip), port), new IPEndPoint(IPAddress.Parse(proxyIP), proxyPort))
        { }

        public Listener(IPEndPoint endPoint, IPEndPoint proxy = null)
        {
            m_tcpListener = new TcpListener(endPoint);
            this.Proxy = proxy;
            m_repeater = new LocalRepeater(this.Proxy);
        }

        public Listener Start()
        {
            this.m_tcpListener.Start(50);
            Logger.Info(this.ToString());
            this.m_tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAccept), m_tcpListener);
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
                try
                {
                    m_repeater.BeginRelay(networkStream);

                    //Http.HttpPackage package = null;
                    //Http.HttpReader reader = new Http.HttpReader();
                    //reader.OnHeaderReady += (h, s) =>
                    //{
                    //    var req = h as Http.HttpRequestHeader;
                    //    if (req.HttpMethod == Http.HttpMethod.Connect)
                    //    {
                    //        m_repeater.BeginRelay(stream, req.Host, req.Port, true);
                    //    }
                    //    else
                    //    {
                    //        m_repeater.BeginRelay(s, req.Host, req.Port, false);
                    //    }
                    //};
                    //for (package = reader.Read(stream);
                    //   package != null;
                    //   package = reader.Read(stream))
                    //{

                    //}
                    /*
                    for (request = HttpPackage.Read(stream);
                        request != null;
                        request = keepAlive ? HttpPackage.Read(stream) : null, response = null, keepAlive = false)
                    {
                        DateTime startTime = DateTime.Now;
                        if (isSsl)
                        {
                            request.Host = host;
                            request.Port = port;
                            request.IsSSL = true;
                        }
                        if (request.HttpMethod == "CONNECT")
                        {
                            stream = SwitchToSslStream(stream, request);
                            isSsl = keepAlive = true;
                            host = request.Host;
                            port = request.Port;
                        }
                        else
                        {
                            this.pendingRequestsCount++;
                            response = this.miner.Fetch(request, this.Proxy, this.Proxy != null);
                            this.pendingRequestsCount--;
                            if (response != null && stream.CanWrite)
                            {
                                stream.Write(response.Binary, 0, response.Length);
                                stream.Flush();
                                // Proxy-Connection: keep-alive
                                keepAlive = request.HeaderItems.ContainsKey("Proxy-Connection")
                                    && String.Compare(request.HeaderItems["Proxy-Connection"], "keep-alive", true) == 0;
                            }
                        }
                    } // end of for
                    */
                }
                catch (Exception ex)
                {
                    throw ex;
                    //Logger.PublishException(ex, request != null ? String.Format("{0}:{1}\n{2}", request.Host, request.Port, request.StartLine) : null);
                }
                finally
                {
                }
            } // end of using
        }

        //private void 

        //private SslStream SwitchToSslStream(Stream stream, HttpPackage request)
        //{
        //    SslStream sslStream = null;
        //    byte[] repBin = ASCIIEncoding.ASCII.GetBytes(String.Format("{0} 200 Connection Established\r\n\r\n", request.Version));
        //    stream.Write(repBin, 0, repBin.Length);
        //    X509Certificate2 cert = CAHelper.GetCertificate(request.Host);
        //    if (cert != null && cert.HasPrivateKey)
        //    {
        //        sslStream = new SslStream(stream, false);
        //        sslStream.AuthenticateAsServer(cert, false, SslProtocols.Tls, true);
        //    }
        //    return sslStream;
        //}
    }
}
