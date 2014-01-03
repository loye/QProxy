using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    public class WebSocketHandler : IHttpHandler
    {
        private const int BUFFER_LENGTH = 4096;

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(HandleWebSocket);
            }
            else
            {
                context.Response.Write(" It's working!");
            }
        }

        #endregion

        private async Task HandleWebSocket(WebSocketContext wsContext)
        {
            string host = wsContext.Headers["Q-Host"];
            int port = int.Parse(wsContext.Headers["Q-Port"]);
            IPAddress ip = IPAddress.TryParse(wsContext.Headers["Q-IP"], out ip) ? ip : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
            bool encrypted = bool.TryParse(wsContext.Headers["Q-Encrypted"], out encrypted) ? encrypted : false;
            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);
            byte[] requestBuffer = new byte[BUFFER_LENGTH];
            WebSocket webSocket = wsContext.WebSocket;

            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);
            using (NetworkStream stream = new NetworkStream(socket, true))
            {

                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(requestBuffer), CancellationToken.None);

                    switch (receiveResult.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            break;
                        case WebSocketMessageType.Binary:
                            await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept binary frame", CancellationToken.None);
                            break;
                        case WebSocketMessageType.Text:
                            stream.Write(requestBuffer, 0, receiveResult.Count);
                            Task localTask = Task.Run(async () =>
                            {
                                byte[] responseBuffer = new byte[BUFFER_LENGTH];
                                for (int len = stream.Read(responseBuffer, 0, responseBuffer.Length); len > 0; len = stream.Read(responseBuffer, 0, responseBuffer.Length))
                                {
                                    await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, len), WebSocketMessageType.Text, false, CancellationToken.None);
                                }
                            });
                            while (receiveResult.EndOfMessage == false)
                            {
                                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(requestBuffer), CancellationToken.None);
                                stream.Write(requestBuffer, 0, receiveResult.Count);
                            }
                            localTask.Wait();



                            //{
                            //    int count = receiveResult.Count;

                            //    while (receiveResult.EndOfMessage == false)
                            //    {
                            //        if (count >= maxMessageSize)
                            //        {
                            //            string closeMessage = string.Format("Maximum message size: {0} bytes.", maxMessageSize);
                            //            await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage, CancellationToken.None);
                            //            return;
                            //        }
                            //        receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, count, maxMessageSize - count), CancellationToken.None);
                            //        count += receiveResult.Count;
                            //    }

                            //    var receivedString = Encoding.UTF8.GetString(receiveBuffer, 0, count);
                            //    var echoString = "You said " + receivedString;
                            //    ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(echoString));

                            //    await socket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            //}
                            break;
                        default:
                            await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, null, CancellationToken.None);
                            break;
                    }
                }
            }
        }
    }
}
