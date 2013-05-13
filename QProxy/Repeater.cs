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

        private Task Transmit(Stream fromStream, Stream toStream)
        {
            Task task = null;
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            bool parseHttp = true;
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = fromStream.Read(buffer, 0, buffer.Length);
                       len > 0;
                       len = fromStream.Read(buffer, 0, buffer.Length))
                {
                    if (toStream != null)
                    {
                        toStream.Write(buffer, 0, len);
                    }
                    if (parseHttp)
                    {
                        HttpPackage package = null;
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
                                        return Task.Run(() =>
                                        {
                                            DirectRelay(fromStream, toStream);
                                        });
                                    }
                                }
                                else
                                {
                                    toStream.Write(bin, 0, (int)mem.Length);
                                }
                                task = Task.Run(() =>
                                {
                                    Transmit(toStream, fromStream);
                                });
                            }
                            if (IsPackageFinished(package))
                            {
                                Console.WriteLine(package.HttpHeader.StartLine);
                            }
                        }
                    }
                }
            }
            return task;
        }


        public void Relay(ref Stream localStream)
        {
            byte[] reqBuf = new byte[HttpPackage.BUFFER_LENGTH];
            byte[] resBuf = new byte[HttpPackage.BUFFER_LENGTH];
            Stream remoteStream = null;

            Task task = Transmit(localStream, remoteStream);
            task.Wait();
            /*
            for (bool hasRequest = true, requestSent = false; hasRequest && localStream.CanRead; requestSent = false)
            {
                hasRequest = false;
                // Request
                using (MemoryStream mem = new MemoryStream())
                {
                    HttpPackage package = null;
                    for (int len = localStream.Read(reqBuf, 0, reqBuf.Length);
                        len > 0;
                        len = localStream.CanRead ? localStream.Read(reqBuf, 0, reqBuf.Length) : 0)
                    {
                        hasRequest = true;
                        if (remoteStream != null)
                        {
                            remoteStream.Write(reqBuf, 0, len);
                            requestSent = true;
                        }

                        mem.Write(reqBuf, 0, len);
                        byte[] bin = mem.GetBuffer();
                        HttpPackage.ValidatePackage(bin, 0, (int)mem.Length, ref package);
                        if (package != null)
                        {
                            // connect & send buffered bytes
                            if (remoteStream == null)
                            {
                                var requestHeader = package.HttpHeader as Http.HttpRequestHeader;
                                remoteStream = this.Connect(ref localStream, requestHeader);
                                if (requestHeader.HttpMethod == HttpMethod.Connect)
                                {
                                    if (!this.DecryptSSL)
                                    {
                                        this.DirectRelay(localStream, remoteStream);
                                        return;
                                    }
                                    break;
                                }
                                else
                                {
                                    remoteStream.Write(bin, 0, (int)mem.Length);
                                    requestSent = true;
                                }
                            }
                            if (IsPackageFinished(package))
                            {
                                break;
                            }
                        }
                    }
                }

                // Response
                if (requestSent && remoteStream.CanRead && localStream.CanWrite)
                {
                    using (MemoryStream mem = new MemoryStream())
                    {
                        HttpPackage package = null;
                        for (int len = remoteStream.Read(resBuf, 0, resBuf.Length);
                            len > 0;
                            len = remoteStream.CanRead ? remoteStream.Read(resBuf, 0, resBuf.Length) : 0)
                        {
                            localStream.Write(resBuf, 0, len);
                            mem.Write(resBuf, 0, len);
                            byte[] bin = mem.GetBuffer();
                            if (HttpPackage.ValidatePackage(bin, 0, (int)mem.Length, ref package) && IsPackageFinished(package))
                            {
                                break;
                            }
                        }
                    }
                }
            }*/
        }

        #region Private Methods

        //private async Task Transmit(Stream from, Stream to, byte[] buffer)
        //{
        //    int len = from.Read(buffer, 0, buffer.Length);
        //    if (len > 0)
        //    {
        //        to.Write(buffer, 0, len);
        //        await Transmit(from, to, buffer);
        //    }
        //}

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
            remoteStream = new NetworkStream(socket, true);

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
