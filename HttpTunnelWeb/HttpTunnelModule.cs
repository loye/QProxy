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
            var response = HttpContext.Current.Response;

            if (String.Compare(workerRequest.GetRawUrl(), "/a", true) == 0)
            {
                if (workerRequest.GetHttpVerbName() == "POST")
                {
                    if (!workerRequest.IsEntireEntityBodyIsPreloaded())
                    {
                        string id = workerRequest.GetUnknownRequestHeader("Q-ID");
                        string action = (workerRequest.GetUnknownRequestHeader("Q-Action") ?? String.Empty).ToLower();

                        workerRequest.SendUnknownResponseHeader("Q-Action", action);
                        workerRequest.SendKnownResponseHeader(HttpWorkerRequest.HeaderConnection, "close");
                        try
                        {
                            switch (action)
                            {
                                case "connect":
                                    {
                                        string host = workerRequest.GetUnknownRequestHeader("Q-Host");
                                        string ipStr = workerRequest.GetUnknownRequestHeader("Q-IP");
                                        int port = int.Parse(workerRequest.GetUnknownRequestHeader("Q-Port"));
                                        IPAddress ip = !String.IsNullOrEmpty(ipStr) ? IPAddress.Parse(ipStr) : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                                        IPEndPoint endPoint = new IPEndPoint(ip, port);

                                        HttpTunnelNode.Connect(id, host, endPoint);

                                        workerRequest.SendUnknownResponseHeader("Q-Message", String.Format("CONNECT: {0} [{1}]", host, endPoint));
                                    }
                                    break;

                                case "write":
                                    {
                                        int length = 0;
                                        byte[] buffer = new byte[4096];
                                        for (int len = workerRequest.ReadEntityBody(buffer, 0, buffer.Length); len > 0; len = workerRequest.ReadEntityBody(buffer, 0, buffer.Length))
                                        {
                                            HttpTunnelNode.Write(id, buffer, 0, len);
                                            length += len;
                                        }
                                        workerRequest.SendUnknownResponseHeader("Q-Message", String.Format("WRITE: {0}", length));
                                    }
                                    break;

                                case "read":
                                    {
                                        int len = 0;
                                        using (Stream outputStream = response.OutputStream)
                                        {
                                            int length;
                                            byte[] buffer = new byte[int.TryParse(workerRequest.GetUnknownRequestHeader("Q-Length"), out length) ? length : 4096];
                                            len = HttpTunnelNode.Read(id, buffer, 0, buffer.Length);
                                            if (len > 0)
                                            {
                                                outputStream.Write(buffer, 0, len);
                                            }
                                            else
                                            {
                                                HttpTunnelNode.Close(id);
                                            }
                                        }
                                        workerRequest.SendUnknownResponseHeader("Q-Message", String.Format("READ: {0}", len));
                                    }
                                    break;
                                case "close":
                                    {
                                        HttpTunnelNode.Close(id);
                                        workerRequest.SendUnknownResponseHeader("Q-Message", "CLOSE");
                                    }
                                    break;
                                default:
                                    throw new NotSupportedException("Action not supported!");
                            }
                        }
                        catch (Exception)
                        {
                        }
                        response.End();
                        workerRequest.CloseConnection();
                    }
                }
                else if (workerRequest.GetHttpVerbName() == "GET")
                {
                    response.Write("HttpTunnelModule is working!");
                    response.End();
                }
            }

        }
    }
    /*
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
     * */
}
