using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    public class HttpTunnelHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            switch (context.Request.HttpMethod)
            {
                case "GET":
                    GET(context);
                    break;
                case "POST":
                    POST(context);
                    break;
                default:
                    break;
            }
        }

        #endregion

        private void GET(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string action = request["Action"] ?? String.Empty;

            switch (action.ToLower())
            {
                case "info":
                    response.Write(HttpTunnelNode.GetDebugInfo().Replace("\r\n", "<br />"));
                    break;

                default:
                    response.Write("It's working!");
                    break;
            }
            response.End();
        }

        /// <summary>
        /// Q-ID, Q-Action
        ///     "CONNECT": Q-Host ,[Q-IP], Q-Port
        ///     "WRITE": 
        ///     "READ": [Q-Length]
        ///     "CLOSE": 
        /// </summary>
        private void POST(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string id = request.Headers["Q-ID"];
            string action = (request.Headers["Q-Action"] ?? String.Empty).ToLower();
            response.Headers["Q-Action"] = action;
            try
            {
                switch (action)
                {
                    case "connect":
                        {
                            string host = request.Headers["Q-Host"];
                            string ip = request.Headers["Q-IP"];
                            int port = int.Parse(request.Headers["Q-Port"]);
                            IPAddress ipAdd = !String.IsNullOrEmpty(ip) ? IPAddress.Parse(ip) : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                            IPEndPoint endPoint = new IPEndPoint(ipAdd, port);
                            HttpTunnelNode.Connect(id, host, endPoint);

                            response.Headers["Q-Message"] = String.Format("CONNECT: {0} [{1}]", host, endPoint);
                        }
                        break;

                    case "write":
                        {
                            int length = 0;
                            using (Stream inputStream = request.InputStream)
                            {
                                byte[] buffer = new byte[4096];
                                for (int len = inputStream.Read(buffer, 0, buffer.Length); len > 0; len = inputStream.Read(buffer, 0, buffer.Length))
                                {
                                    HttpTunnelNode.Write(id, buffer, 0, len);
                                    length += len;
                                }
                            }
                            response.Headers["Q-Message"] = String.Format("WRITE: {0}", length);
                        }
                        break;

                    case "read":
                        {
                            int len = 0;
                            using (Stream outputStream = response.OutputStream)
                            {
                                int length;
                                byte[] buffer = new byte[int.TryParse(request.Headers["Q-Length"], out length) ? length : 4096];
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
                            response.Headers["Q-Message"] = String.Format("READ: {0}", len);
                        }
                        break;

                    case "close":
                        {
                            HttpTunnelNode.Close(id);
                            response.Headers["Q-Message"] = "CLOSE";
                        }
                        break;
                    case "debug":
                        {
                            response.Headers["Q-Message"] = "Debug";
                        }
                        break;
                    default:
                        throw new NotSupportedException("Action not supported!");
                }
            }
            catch (Exception e)
            {
                HttpTunnelNode.Close(id);
                response.Clear();
                response.Headers["Q-Exception"] = e.GetType().Name;
                response.Headers["Q-Message"] = "Exception";
                response.Write(e.Message);
            }
            response.End();
        }
    }
}
