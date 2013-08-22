using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace Q.Net.Web
{
    public class HttpTunnelHandler : IHttpHandler
    {
        private const int BUFFER_LENGTH = 4096;

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
            response.Write("It's working!");
        }

        /// <summary>
        /// RequestHeaders:
        ///     Q-ID
        ///     Q-Action:
        ///         "CONNECT": Q-Host, Q-Port ,[Q-IP]
        ///         "WRITE": 
        ///         "READ": [Q-Length]
        ///         "CLOSE": 
        /// ResponseHeaders:
        ///     Q-Action
        ///     Q-Message
        /// </summary>
        private void POST(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string debugMessage = null;
            string id = request.Headers["Q-ID"];
            string action = (request.Headers["Q-Action"] ?? String.Empty).ToLower();
            try
            {
                switch (action)
                {
                    case "connect":
                        {
                            string host = request.Headers["Q-Host"];
                            int port = int.Parse(request.Headers["Q-Port"]);
                            IPAddress ip = IPAddress.TryParse(request.Headers["Q-IP"], out ip) ? ip : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                            bool encrypted = bool.TryParse(request.Headers["Q-Encrypted"], out encrypted) ? encrypted : false;
                            HttpTunnelNode.Instance.Connect(id, host, new IPEndPoint(ip, port), encrypted);

                            debugMessage = String.Format("CONNECT: {0} [{1}:{2}]", host, ip, port);
                        }
                        break;

                    case "write":
                        {
                            int length = 0;
                            using (Stream inputStream = request.InputStream)
                            {
                                byte[] buffer = new byte[BUFFER_LENGTH];
                                for (int len = inputStream.Read(buffer, 0, buffer.Length); len > 0; len = inputStream.Read(buffer, 0, buffer.Length))
                                {
                                    HttpTunnelNode.Instance.Write(id, buffer, 0, len, length);
                                    length += len;
                                }
                            }
                            debugMessage = String.Format("WRITE: {0}", length);
                        }
                        break;

                    case "read":
                        {
                            int bufLength = int.TryParse(request.Headers["Q-Length"], out bufLength) ? bufLength : BUFFER_LENGTH;
                            int length = 0;
                            using (Stream outputStream = response.OutputStream)
                            {
                                byte[] buffer = new byte[bufLength];
                                length = HttpTunnelNode.Instance.Read(id, buffer, 0, buffer.Length);
                                if (length > 0)
                                {
                                    outputStream.Write(buffer, 0, length);
                                }
                                else
                                {
                                    HttpTunnelNode.Instance.Close(id);
                                }
                            }
                            debugMessage = String.Format("READ: {0}", length);
                        }
                        break;

                    case "close":
                        {
                            HttpTunnelNode.Instance.Close(id);
                            debugMessage = "CLOSE";
                        }
                        break;
                    case "debug":
                        {
                            debugMessage = "Debug";
                        }
                        break;
                    default:
                        throw new NotSupportedException("Action not supported!");
                }
            }
            catch (Exception e)
            {
                HttpTunnelNode.Instance.Close(id);
                response.Clear();
                response.Headers["Q-Exception"] = e.GetType().Name;
                debugMessage = "Exception";
                response.Write(e.Message);
                response.Write(e.Source);
                response.Write(e.StackTrace);
            }
            response.Headers["Q-Message"] = debugMessage;
            response.Headers["Q-Action"] = action;
            response.End();
        }
    }
}
