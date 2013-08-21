using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    [Obsolete("Not used anymore")]
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
            HttpWorkerRequest wr = (HttpWorkerRequest)provider.GetService(typeof(HttpWorkerRequest));
            var response = HttpContext.Current.Response;

            if (String.Compare(wr.GetRawUrl(), "/a", true) == 0)
            {
                if (wr.GetHttpVerbName() == "POST")
                {
                    if (!wr.IsEntireEntityBodyIsPreloaded())
                    {
                        wr.SendKnownResponseHeader(HttpWorkerRequest.HeaderConnection, "close");

                        try
                        {
                            using (Stream remoteStream = CreateRemoteStream(wr))
                            using (Stream responseStream = response.OutputStream)
                            {
                                var remoteTask = Task.Run(() =>
                                {
                                    byte[] buffer = new byte[4096];
                                    for (int len = remoteStream.Read(buffer, 0, buffer.Length); len > 0 && wr.IsClientConnected(); len = remoteStream.Read(buffer, 0, buffer.Length))
                                    {
                                        responseStream.Write(buffer, 0, len);
                                        response.Flush();
                                    }
                                });
                                var localTask = Task.Run(() =>
                                {
                                    byte[] buffer = new byte[4096];
                                    for (int len = wr.ReadEntityBody(buffer, 0, buffer.Length); len > 0; len = wr.ReadEntityBody(buffer, 0, buffer.Length))
                                    {
                                        remoteStream.Write(buffer, 0, len);
                                    }
                                    remoteTask.Wait();
                                });
                                Task.WaitAny(localTask, remoteTask);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        response.End();
                        wr.CloseConnection();
                    }
                }
                else if (wr.GetHttpVerbName() == "GET")
                {
                    response.Write("HttpTunnelModule is working!");
                    response.End();
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
