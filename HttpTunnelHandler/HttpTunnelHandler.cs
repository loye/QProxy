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

        #endregion

        private void GET(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Write("It's working!");
            response.End();
        }

        private void POST(HttpContext context)
        {
            int len = 0;
            len++;


            var request = context.Request;
            var response = context.Response;
            response.Write("xxx");
            response.Flush();

            /*
            string host = request.Headers["Q-Host"];
            int port = int.Parse(request.Headers["Q-Port"]);
            IPAddress ip = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
            IPEndPoint endPoint = new IPEndPoint(ip, port);  //new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);//
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);

            using (NetworkStream remoteStream = new NetworkStream(socket, true))
            using (Stream inputStream = request.InputStream)
            using (Stream outputStream = response.OutputStream)
            {
                Task t1 = Task.Run(() => { Transfer(inputStream, remoteStream); });
                Task t2 = Task.Run(() => { Transfer(remoteStream, outputStream, response.Flush); });
                Task.WaitAll(t1, t2);
            }
             * */
            //using (Stream inputStream = request.InputStream)
            //using (Stream outputStream = response.OutputStream)
            //{
            //    Transfer(inputStream, outputStream, response.Flush);
            //}

            response.End();
        }

        private void Transfer(Stream src, Stream dest, Action action = null)
        {
            byte[] buffer = new byte[4096];
            for (int len = src.Read(buffer, 0, buffer.Length); len > 0; len = src.Read(buffer, 0, buffer.Length))
            {
                dest.Write(buffer, 0, len);
                if (action != null) action();
            }
        }
    }
}
