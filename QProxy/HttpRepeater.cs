using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Q.Net;

namespace Q.Proxy
{
    public class HttpRepeater : Repeater
    {
        public bool DecryptSSL { get; set; }

        public HttpRepeater(IPEndPoint proxy = null, bool decryptSSL = false)
            : base(proxy)
        {
            this.DecryptSSL = decryptSSL;
        }

        public override void Relay(Stream localStream)
        {
            Transmit(localStream, null);
        }

        #region Private Methods

        private void Transmit(Stream fromStream, Stream toStream)
        {
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            Task task = null;
            bool headerComplete = false;
            HttpPackage package = null;
            MemoryStream mem = new MemoryStream();

            for (int len = fromStream.Read(buffer, 0, buffer.Length); len > 0; len = fromStream.Read(buffer, 0, buffer.Length))
            {
                if (toStream != null)
                {
                    toStream.Write(buffer, 0, len);
                }
                mem.Write(buffer, 0, len);
                HttpPackage.ValidatePackage(mem.GetBuffer(), 0, (int)mem.Length, ref package);
                if (package != null)
                {
                    if (!headerComplete)
                    {
                        headerComplete = true;
                        if (package.HttpHeader is Net.HttpRequestHeader)
                        {
                            var requestHeader = package.HttpHeader as Net.HttpRequestHeader;
                            //if (this.Proxy == null && requestHeader[HttpHeaderKey.Proxy_Connection] == "keep-alive")
                            //{
                            //    requestHeader[HttpHeaderKey.Proxy_Connection] = null;
                            //    requestHeader[HttpHeaderKey.Connection] = "keep-alive";
                            //}
                            if (toStream == null)
                            {
                                toStream = this.Connect(ref fromStream, requestHeader);
                                if (requestHeader.HttpMethod == HttpMethod.Connect)
                                {
                                    if (!this.DecryptSSL)
                                    {
                                        DirectRelay(fromStream, toStream);
                                        break;
                                    }
                                    else
                                    {
                                        task = Task.Run(() => { Transmit(toStream, fromStream); });
                                        headerComplete = false;
                                        package = null;
                                        mem.Dispose();
                                        mem = new MemoryStream();
                                        continue;
                                    }
                                }
                                else
                                {
                                    byte[] reqBin = package.ToBinary();
                                    toStream.Write(reqBin, 0, reqBin.Length);
                                    task = Task.Run(() => { Transmit(toStream, fromStream); });
                                }
                            }
                        }
                    }

                    if (package.IsCompleted)
                    {
                        Console.WriteLine(package.HttpHeader.StartLine);
                        mem.Dispose();
                        break;
                    }
                }
            } // end of for

            if (task != null)
            {
                task.Wait();
            }
        }

        private void DirectRelay(Stream localStream, Stream remoteStream)
        {
            Task.WaitAll(
                localStream.CopyToAsync(remoteStream, HttpPackage.BUFFER_LENGTH),
                remoteStream.CopyToAsync(localStream, HttpPackage.BUFFER_LENGTH));
        }

        private Stream Connect(ref Stream localStream, Net.HttpRequestHeader requestHeader)
        {
            IPEndPoint endPoint = this.Proxy ?? new IPEndPoint(DnsHelper.GetHostAddress(requestHeader.Host), requestHeader.Port);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream remoteStream = new NetworkStream(socket, true);
            if (requestHeader.HttpMethod == HttpMethod.Connect)
            {
                var remoteTask = HttpsConnector.Instance.ConnectAsClientAsync(remoteStream, requestHeader.Host, requestHeader.Port, this.Proxy, this.DecryptSSL);
                var localTask = HttpsConnector.Instance.ConnectAsServerAsync(localStream, requestHeader.Host, this.DecryptSSL);
                Task.WaitAll(remoteTask, localTask);
                remoteStream = remoteTask.Result;
                localStream = localTask.Result;
            }
            return remoteStream;
        }

        #endregion
    }
}
