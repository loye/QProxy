using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Q.Configuration;
using Q.Net;

namespace Q.Proxy
{
    public class HttpRepeater : Repeater
    {
        public bool DecryptSSL { get; set; }

        public HttpRepeater(listener listenerConfig)
            : base(listenerConfig)
        {
            this.DecryptSSL = listenerConfig.decryptSSL;
        }

        public override void Relay(Stream localStream)
        {
            Stream remoteStream = null;
            bool transparent = false;

            var remoteTask = new Task(() =>
            {
                Transfer(ref remoteStream, ref localStream, transparent);
            });
            var localTask = Task.Run(() =>
            {
                Transfer(ref localStream, ref remoteStream, transparent, (t) => { transparent = t; remoteTask.Start(); });
            });

            Task.WaitAll(remoteTask, localTask);
            if (remoteStream != null)
            {
                remoteStream.Dispose();
            }
        }

        #region Private Methods

        private void Transfer(ref Stream src, ref Stream dest, bool transparent = false, Action<bool> startRemoteTransfer = null)
        {
            byte[] buffer = new byte[BUFFER_LENGTH];
            for (int pkgCount = 0; pkgCount < Int32.MaxValue; pkgCount++)
            {
                if (transparent)
                {
                    for (int len = src.Read(buffer, 0, buffer.Length); len > 0; len = src.Read(buffer, 0, buffer.Length))
                    {
                        dest.Write(buffer, 0, len);
                    }
                    break;
                }
                else
                {
                    HttpPackage package = null;
                    bool headerCompleted = false;
                    using (MemoryStream mem = new MemoryStream())
                    {
                        for (int len = src.Read(buffer, 0, buffer.Length); len > 0; len = src.Read(buffer, 0, buffer.Length))
                        {
                            if (dest != null)
                            {
                                dest.Write(buffer, 0, len);
                            }
                            mem.Write(buffer, 0, len);
                            HttpPackage.ValidatePackage(mem.GetBuffer(), 0, (int)mem.Length, ref package);
                            if (package != null)
                            {
                                if (!headerCompleted)
                                {
                                    headerCompleted = true;
                                    var requestHeader = package.HttpHeader as Q.Net.HttpRequestHeader;
                                    if (dest == null && requestHeader != null)
                                    {
                                        dest = this.Connect(ref src, requestHeader);
                                        if (requestHeader.HttpMethod == HttpMethod.Connect)
                                        {
                                            if (!this.DecryptSSL)
                                            {
                                                transparent = true;
                                            }
                                        }
                                        else
                                        {
                                            if (this.Proxy == null && String.Compare(requestHeader[HttpHeaderKey.Proxy_Connection], HttpHeaderValue.Connection.Keep_Alive, true) == 0)
                                            {
                                                requestHeader[HttpHeaderKey.Proxy_Connection] = null;
                                                requestHeader[HttpHeaderKey.Connection] = "keep-alive";
                                            }
                                            byte[] reqBin = package.ToBinary();
                                            dest.Write(reqBin, 0, reqBin.Length);
                                        }
                                        startRemoteTransfer(transparent);
                                    }
                                }
                                if (transparent || package.IsCompleted) // end of Package
                                {
                                    Console.WriteLine(package.HttpHeader.StartLine);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private Stream Connect(ref Stream localStream, Q.Net.HttpRequestHeader requestHeader)
        {
            Stream remoteStream = GetStream(requestHeader.Host, requestHeader.Port);

            if (requestHeader.HttpMethod == HttpMethod.Connect)
            {
                var remoteTask = HttpsConnector.Instance.ConnectAsClientAsync(remoteStream, requestHeader.Host, requestHeader.Port, this.DecryptSSL, this.Proxy != null); // TODO
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
