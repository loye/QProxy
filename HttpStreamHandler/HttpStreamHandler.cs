using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Q.Net.Web
{
    public class HttpStreamHandler : IHttpHandler
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
                case "POST":
                    POST(context);
                    break;
                case "GET":
                    GET(context);
                    break;
                default:
                    break;
            }
        }

        private void GET(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Headers["Connection"] = "close";
            response.Write("ABC");
            response.Flush();
            response.Write("DEF");
            response.End();
        }

        private void POST(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string host = request.Headers["Q-Host"];
            int port = int.Parse(request.Headers["Q-Port"]);
            IPAddress ip = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
            IPEndPoint endPoint = new IPEndPoint(ip, port);  //new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);//
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            using (Stream remoteStream = new NetworkStream(socket, true))
            using (Stream inputStream = request.InputStream)
            using (Stream outputStream = response.OutputStream)
            {
                byte[] buffer = new byte[4096];
                for (int len = inputStream.Read(buffer, 0, buffer.Length); len > 0; len = inputStream.Read(buffer, 0, buffer.Length))
                {
                    remoteStream.Write(buffer, 0, len);
                }
                for (int len = remoteStream.Read(buffer, 0, buffer.Length); len > 0; len = remoteStream.Read(buffer, 0, buffer.Length))
                {
                    outputStream.Write(buffer, 0, len);
                }
            }

            response.End();
        }

        #endregion
    }
}
