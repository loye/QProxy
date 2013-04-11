using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Q.Http;

namespace Q.Proxy
{
    public abstract class Repeater
    {
        //Acceptor
        //Transmitter

        protected IPEndPoint Proxy { get; set; }

        public Repeater(IPEndPoint proxy = null)
        {
            this.Proxy = proxy;
        }

        public abstract void BeginRelay(Stream localStream);
    }

    public class LocalRepeater : Repeater
    {
        public LocalRepeater(IPEndPoint proxy = null) : base(proxy) { }

        public override void BeginRelay(Stream localStream)
        {
            var acceptor = new HttpAcceptor();
            byte[] recievedBytes;
            int length;
            Http.HttpRequestHeader requestHeader = acceptor.Accept(localStream, out recievedBytes, out length) as Http.HttpRequestHeader;
            if (requestHeader != null)
            {
                string host = requestHeader.Host;
                int port = requestHeader.Port;
                bool SSL = requestHeader.HttpMethod == HttpMethod.Connect;
                bool byProxy = false; // this.Proxy != null;
                IPEndPoint endPoint = byProxy ? this.Proxy : new IPEndPoint(DnsHelper.GetHostAddress(host), port);
                Stream remoteStream = new HttpStream(endPoint);
                if (SSL)
                {
                    var res = new Http.HttpResponseHeader(200, Http.HttpStatus.Connection_Established);
                    byte[] resBin = res.ToBinary();
                    localStream.Write(resBin, 0, resBin.Length);
                }
                else
                {
                    remoteStream.Write(recievedBytes, 0, length);
                }
                localStream.CopyTo(remoteStream);
                remoteStream.CopyTo(localStream);
            }
        }
    }

}
