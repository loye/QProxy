using Q.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Proxy
{
    public class Repeater
    {
        public IPEndPoint Proxy { get; set; }

        public bool DecryptSSL { get; set; }

        public Repeater(IPEndPoint proxy = null)
        {
            this.Proxy = proxy;
        }

        private async Task Transmit(Stream fromStream, Stream toStream)
        {
            int count = 0;
            Task task = null;
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            bool parseHttp = true;
            HttpPackage package = null;
            using (MemoryStream mem = new MemoryStream())
            {
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
                                }
                                task = Task.Run(async () => { await Transmit(toStream, fromStream); });
                                Console.WriteLine(package.HttpHeader.StartLine);
                            }
                            if (IsPackageFinished(package))
                            {
                                count++;
                                //Console.WriteLine(package.HttpHeader.StartLine);
                            }
                        }
                    }
                }
            }
            if (task != null)
            {
                await task;
            }
            Console.WriteLine("Transmit quit with count: " + count);
        }


        public async Task Relay(Stream localStream)
        {
            using (Stream remoteStream = null)
            {
                await Transmit(localStream, remoteStream);
            }
        }

        #region Private Methods

        private bool IsPackageFinished(HttpPackage package)
        {
            if (package != null && package.IsValid)
            {
                if (package.HttpHeader.ContentLength == 0
                    && String.Compare(package.HttpHeader[HttpHeaderKey.Connection], "close", true) == 0) // Connection: close
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
            Stream remoteStream;
            IPEndPoint endPoint = this.Proxy ?? new IPEndPoint(DnsHelper.GetHostAddress(requestHeader.Host), requestHeader.Port);

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            remoteStream = new NetworkStream(socket, false);

            if (requestHeader.HttpMethod == HttpMethod.Connect)
            {
                ConnectBySSL(ref localStream, ref remoteStream, requestHeader);
            }

            return remoteStream;
        }

        private void ConnectBySSL(ref Stream localStream, ref Stream remoteStream, Http.HttpRequestHeader requestHeader)
        {
            // Send connect request to http proxy server
            if (this.Proxy != null)
            {
                byte[] requestBin = requestHeader.ToBinary();
                remoteStream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = HttpPackage.Parse(remoteStream);
                if (response == null || (response.HttpHeader as Http.HttpResponseHeader).StatusCode != 200)
                {
                    throw new Exception(String.Format("Connect to proxy server[{0}:{1}] with SSL failed!", requestHeader.Host, requestHeader.Port));
                }
            }

            // Send connected response to local
            var res = new Http.HttpResponseHeader(200, Http.HttpStatus.Connection_Established, requestHeader.Version);
            byte[] responseBin = res.ToBinary();
            localStream.Write(responseBin, 0, responseBin.Length);

            // Decrypt SSL
            if (this.DecryptSSL)
            {
                var t1 = SwitchToSslStreamAsClientAsync(remoteStream, requestHeader.Host);
                var t2 = SwitchToSslStreamAsServerAsync(localStream, requestHeader.Host);
                Task.WaitAll(t1, t2);
                remoteStream = t1.Result;
                localStream = t2.Result;
            }
        }

        private async Task<SslStream> SwitchToSslStreamAsClientAsync(Stream stream, string host)
        {
            SslStream ssltream = new SslStream(stream, false);
            await ssltream.AuthenticateAsClientAsync(host);
            return ssltream;
        }

        private async Task<SslStream> SwitchToSslStreamAsServerAsync(Stream stream, string host)
        {
            SslStream sslStream = new SslStream(stream, false);
            await sslStream.AuthenticateAsServerAsync(CAHelper.GetCertificate(host), false, SslProtocols.Tls, true);
            return sslStream;
        }

        #endregion

        #endregion
    }
}
