using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    public class WebSocketTunnelHandler : IHttpHandler
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
            WebSocket webSocket = wsContext.WebSocket;
            try
            {
                string host = wsContext.Headers["Q-Host"];
                int port = int.Parse(wsContext.Headers["Q-Port"]);
                IPAddress ip = IPAddress.TryParse(wsContext.Headers["Q-IP"], out ip) ? ip : Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                //bool encrypted = bool.TryParse(wsContext.Headers["Q-Encrypted"], out encrypted) ? encrypted : false;
                IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);

                Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(remoteEndPoint);
                using (NetworkStream remoteStream = new NetworkStream(socket, true))
                {
                    await Transfer(webSocket, remoteStream);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task Transfer(WebSocket webSocket, Stream remoteStream)
        {
            byte[] requestBuffer = new byte[BUFFER_LENGTH];
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(requestBuffer), CancellationToken.None);

            switch (receiveResult.MessageType)
            {
                case WebSocketMessageType.Close:
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;

                case WebSocketMessageType.Binary:
                    await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept binary frame", CancellationToken.None);
                    break;

                case WebSocketMessageType.Text:
                    remoteStream.Write(requestBuffer, 0, receiveResult.Count);
                    Task localTask = Task.Run(async () =>
                    {
                        byte[] responseBuffer = new byte[BUFFER_LENGTH];
                        for (int len = remoteStream.Read(responseBuffer, 0, responseBuffer.Length); len > 0; len = remoteStream.Read(responseBuffer, 0, responseBuffer.Length))
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, len), WebSocketMessageType.Text, len < BUFFER_LENGTH, CancellationToken.None);
                        }
                    });
                    while (webSocket.State == WebSocketState.Open)
                    {
                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(requestBuffer), CancellationToken.None);
                        remoteStream.Write(requestBuffer, 0, receiveResult.Count);
                    }
                    localTask.Wait();
                    break;

                default:
                    await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, null, CancellationToken.None);
                    break;
            }
        }
    }
}
