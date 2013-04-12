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
using Q.Http;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        //Acceptor
        //Transmitter

        public IPEndPoint Proxy { get; set; }

        public bool DecryptSSL { get; set; }

        public Repeater(IPEndPoint proxy = null)
        {
            this.Proxy = proxy;
        }

        public abstract void Relay(Stream localStream, BufferPool bufferPool, Http.HttpRequestHeader requestHeader);


    }

    public class LocalRepeater : Repeater
    {
        private const int BUFFER_LENGTH = 1024;


        private byte[] remoteBuffer = new byte[BUFFER_LENGTH];

        public LocalRepeater(IPEndPoint proxy = null) : base(proxy) { }

        public override void Relay(Stream localStream, BufferPool bufferPool, Http.HttpRequestHeader requestHeader)
        {
            bool SSL = requestHeader.HttpMethod == HttpMethod.Connect;
            Stream remoteStream = this.Connect(ref localStream, requestHeader, this.Proxy);

            if (SSL)
            {
                if (this.DecryptSSL)
                {
                    return;
                }
            }
            else
            {
                byte[] bin = bufferPool.ReadAllBytes();
                remoteStream.Write(bin, 0, bin.Length);
            }

            int cnt = 0;
            byte[] buffer = new byte[BUFFER_LENGTH];
            do
            {
                cnt = 0;
                for (int len = localStream.Read(buffer, 0, buffer.Length);
                               len > 0;
                               len = localStream.CanRead ? localStream.Read(buffer, 0, buffer.Length) : 0)
                {
                    remoteStream.Write(buffer, 0, len);
                    cnt++;
                }
                for (int len = remoteStream.Read(buffer, 0, buffer.Length);
                               len > 0;
                               len = remoteStream.CanRead ? remoteStream.Read(buffer, 0, buffer.Length) : 0)
                {
                    localStream.Write(buffer, 0, len);
                    cnt++;
                }
            } while (cnt > 0);
        }

        private Stream Connect(ref Stream localStream, Http.HttpRequestHeader requestHeader, IPEndPoint proxy)
        {
            Stream remoteStream;
            string host = requestHeader.Host;
            int port = requestHeader.Port;
            bool SSL = requestHeader.HttpMethod == HttpMethod.Connect;
            bool byProxy = this.Proxy != null;
            bool decryptSSL = this.DecryptSSL;
            IPEndPoint endPoint = byProxy ? this.Proxy : new IPEndPoint(DnsHelper.GetHostAddress(host), port);

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            remoteStream = new NetworkStream(socket, true);

            if (SSL)
            {
                ConnectBySSL(ref localStream, ref remoteStream, requestHeader, byProxy, decryptSSL);
            }

            return remoteStream;
        }

        private void ConnectBySSL(ref Stream localStream, ref Stream remoteStream, Http.HttpRequestHeader connectHeader, bool connectToProxy, bool decryptSSL)
        {
            string host = connectHeader.Host;
            int port = connectHeader.Port;
            string version = connectHeader.Version;

            // Send connect request to http proxy server
            if (connectToProxy)
            {
                byte[] requestBin = connectHeader.ToBinary();
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
            if (decryptSSL)
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
    }
}
