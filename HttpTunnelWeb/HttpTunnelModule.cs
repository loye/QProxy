using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    public class HttpTunnelModule : IHttpModule
    {
        #region IHttpModule Members

        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(this.Application_BeginRequest);
        }

        public void Dispose()
        {
        }

        #endregion

        public void Application_BeginRequest(Object source, EventArgs e)
        {
            IServiceProvider provider = (IServiceProvider)HttpContext.Current;
            HttpWorkerRequest workerRequest = (HttpWorkerRequest)provider.GetService(typeof(HttpWorkerRequest));

            if (workerRequest.GetHttpVerbName() == "POST" && workerRequest.GetUnknownRequestHeader("Q-Type") == "HttpTunnel")
            {
                if (!workerRequest.IsEntireEntityBodyIsPreloaded())
                {
                    var response = HttpContext.Current.Response;
                    workerRequest.SendKnownResponseHeader(HttpWorkerRequest.HeaderConnection, "close");
                    workerRequest.SendUnknownResponseHeader("Q-Success", "true");

                    using (Stream remoteStream = CreateRemoteStream(workerRequest))
                    using (Stream responseStream = response.OutputStream)
                    {
                        Task.WaitAny(
                            Task.Run(() =>
                            {
                                Transfer(workerRequest.ReadEntityBody, remoteStream.Write);
                            }),
                            Task.Run(() =>
                            {
                                Transfer(remoteStream.Read, responseStream.Write, response.Flush);
                            }));
                    }
                    response.End();
                }
            }
        }

        private void Transfer(Func<byte[], int, int, int> read, Action<byte[], int, int> write, Action flush = null)
        {
            byte[] buffer = new byte[4096];
            for (int len = read(buffer, 0, buffer.Length); len > 0; len = read(buffer, 0, buffer.Length))
            {
                write(buffer, 0, len);
                if (flush != null)
                {
                    flush();
                }
            }
        }

        private Stream CreateRemoteStream(HttpWorkerRequest workerRequest)
        {
            string host = workerRequest.GetUnknownRequestHeader("Q-Host");
            string ipStr = workerRequest.GetUnknownRequestHeader("Q-IP");
            int port = int.Parse(workerRequest.GetUnknownRequestHeader("Q-Port"));
            IPAddress ip = !String.IsNullOrEmpty(ipStr) ? IPAddress.Parse(ipStr) : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
            IPEndPoint endPoint = new IPEndPoint(ip, port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            return new NetworkStream(socket, true);
        }
    }
}
