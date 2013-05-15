using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Q.Http;

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
            int requestCount = Transmit(localStream, null);
            Console.WriteLine(requestCount);
        }

        #region Private Methods

        private int Transmit(Stream fromStream, Stream toStream)
        {
            int count = 0;
            Task task = null;
            bool parseHttp = true;
            bool headerComplete = false;
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            HttpPackage package = null;
            MemoryStream mem = new MemoryStream();

            for (int len = fromStream.Read(buffer, 0, buffer.Length); len > 0; len = fromStream.Read(buffer, 0, buffer.Length))
            {
                if (toStream != null)
                {
                    toStream.Write(buffer, 0, len);
                }
                // Parse Http
                if (parseHttp)
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    HttpPackage.ValidatePackage(bin, 0, (int)mem.Length, ref package);
                    if (package != null)
                    {
                        if (!headerComplete)
                        {
                            headerComplete = true;
                            var requestHeader = package.HttpHeader as Http.HttpRequestHeader;
                            if (requestHeader != null)
                            {
                                if (requestHeader[HttpHeaderKey.Proxy_Connection] == "keep-alive")
                                {
                                    //if (this.Proxy == null)
                                    //{
                                    //    requestHeader[HttpHeaderKey.Proxy_Connection] = null;
                                    //    requestHeader[HttpHeaderKey.Connection] = "keep-alive";
                                    //}
                                }

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
                                    }
                                    else
                                    {
                                        byte[] reqBin = package.ToBinary();
                                        toStream.Write(reqBin, 0, reqBin.Length);
                                    }
                                    task = Task.Run(() =>
                                    {
                                        Transmit(toStream, fromStream);
                                    });
                                }
                            }
                        }

                        if (IsPackageFinished(package))
                        {
                            Console.WriteLine(package.HttpHeader);
                            count++;
                            package = null;
                            headerComplete = false;
                            mem.Dispose();
                            mem = new MemoryStream();
                        }
                    }
                }
            }

            if (task != null)
            {
                task.Wait();
                toStream.Dispose();
            }
            return count;
        }

        private bool IsPackageFinished(HttpPackage package)
        {
            if (package != null && package.IsValid)
            {
                if (package.HttpHeader is Http.HttpRequestHeader
                    && (package.HttpHeader as Http.HttpRequestHeader).HttpMethod == HttpMethod.Connect)
                {
                    return true;
                }
                if (package.HttpHeader.ContentLength == 0
                     && String.Compare(package.HttpHeader[HttpHeaderKey.Connection], "close", true) == 0)
                {
                    Console.WriteLine("Connection: close");
                    return false;
                }
                return true;
            }
            return false;
        }

        private void DirectRelay(Stream localStream, Stream remoteStream)
        {
            Task.WaitAll(
                localStream.CopyToAsync(remoteStream, HttpPackage.BUFFER_LENGTH),
                remoteStream.CopyToAsync(localStream, HttpPackage.BUFFER_LENGTH));
        }

        #region Connect

        private Stream Connect(ref Stream localStream, Http.HttpRequestHeader requestHeader)
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

        #endregion
    }
}
