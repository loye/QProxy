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

        public void Relay(ref Stream localStream)
        {
            byte[] buffer = new byte[HttpPackage.BUFFER_LENGTH];
            Stream remoteStream = null;

            for (bool hasRequest = true, requestSent = false; hasRequest && localStream.CanRead; requestSent = false)
            {
                hasRequest = false;
                // Request
                using (MemoryStream mem = new MemoryStream())
                {
                    HttpPackage package = null;
                    for (int len = localStream.Read(buffer, 0, buffer.Length);
                        len > 0;
                        len = localStream.CanRead ? localStream.Read(buffer, 0, buffer.Length) : 0)
                    {
                        hasRequest = true;
                        if (remoteStream != null)
                        {
                            remoteStream.Write(buffer, 0, len);
                            requestSent = true;
                        }

                        mem.Write(buffer, 0, len);
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
                                    //this.DirectRelay(localStream, remoteStream);
                                    //return;
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
                        for (int len = remoteStream.Read(buffer, 0, buffer.Length);
                            len > 0;
                            len = remoteStream.CanRead ? remoteStream.Read(buffer, 0, buffer.Length) : 0)
                        {
                            localStream.Write(buffer, 0, len);
                            mem.Write(buffer, 0, len);
                            byte[] bin = mem.GetBuffer();
                            if (HttpPackage.ValidatePackage(bin, 0, (int)mem.Length, ref package) && IsPackageFinished(package))
                            {
                                break;
                            }
                        }
                    }
                }
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

        private async Task Transmit(Stream from, Stream to, byte[] buffer)
        {
            await from.ReadAsync(buffer, 0, buffer.Length).ContinueWith(async (alen) =>
            {
                int len = await alen;
                if (len > 0)
                {
                    to.Write(buffer, 0, len);
                    await Transmit(from, to, buffer);
                }
            });
        }

        private void DirectRelay(Stream localStream, Stream remoteStream)
        {
            byte[] buf1 = new byte[HttpPackage.BUFFER_LENGTH];
            byte[] buf2 = new byte[HttpPackage.BUFFER_LENGTH];

            Task.WaitAll(
                Transmit(localStream, remoteStream, buf1),
                Transmit(remoteStream, localStream, buf2));
        }

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

        #region Https Connection

        private void ConnectBySSL(ref Stream localStream, ref Stream remoteStream, Http.HttpRequestHeader requestHeader)
        {
            string host = requestHeader.Host;
            int port = requestHeader.Port;
            string version = requestHeader.Version;

            // Send connect request to http proxy server
            if (this.Proxy != null)
            {
                byte[] requestBin = requestHeader.ToBinary();
                remoteStream.Write(requestBin, 0, requestBin.Length);
                HttpPackage response = HttpPackage.Parse(remoteStream);
                if (response == null || (response.HttpHeader as Http.HttpResponseHeader).StatusCode != 200)
                {
                    throw new Exception(String.Format("SwitchToSslStreamAsClient: Connect to proxy server[{0}:{1}] with SSL failed!", host, port));
                }
            }

            // Send connected response to local
            var res = new Http.HttpResponseHeader(200, Http.HttpStatus.Connection_Established, version);
            byte[] responseBin = res.ToBinary();
            localStream.Write(responseBin, 0, responseBin.Length);

            // Decrypt SSL
            if (this.DecryptSSL)
            {
                var t1 = SwitchToSslStreamAsClientAsync(remoteStream, host);
                var t2 = SwitchToSslStreamAsServerAsync(localStream, host);
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
            SslStream sslStream = null;
            X509Certificate2 cert = CAHelper.GetCertificate(host);
            if (cert != null && cert.HasPrivateKey)
            {
                sslStream = new SslStream(stream, false);
                await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls, true);
            }
            return sslStream;
        }

        #endregion

        #endregion
    }
}
