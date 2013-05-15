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
            using (Stream remoteStream = null)
            {
                Transmit(localStream, remoteStream);
            }
        }

        #region Private Methods

        private void Transmit(Stream fromStream, Stream toStream)
        {
            Task task = null;
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            bool parseHttp = true;
            HttpPackage package = null;
            using (MemoryStream mem = new MemoryStream())
            {
                fromStream.ReadTimeout = 300000;
                for (int len = fromStream.Read(buffer, 0, buffer.Length); len > 0; len = fromStream.Read(buffer, 0, buffer.Length))
                {
                    if (toStream != null)
                    {
                        toStream.Write(buffer, 0, len);
                    }
                    if (parseHttp)
                    {
                        mem.Write(buffer, 0, len);
                        byte[] bin = mem.GetBuffer();
                        HttpPackage.ValidatePackage(bin, 0, (int)mem.Length, ref package);
                        if (package != null)
                        {
                            if (toStream == null)
                            {
                                var requestHeader = package.HttpHeader as Http.HttpRequestHeader;
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
                                    toStream.Write(bin, 0, (int)mem.Length);
                                    task = Task.Run(() =>
                                    {
                                        Transmit(toStream, fromStream);
                                    });
                                }
                            }
                            if (IsPackageFinished(package))
                            {
                                Console.WriteLine(package.HttpHeader.StartLine);
                                break;
                            }
                        }
                    }
                }
            }

            if (task != null)
            {
                task.Wait();
            }
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
