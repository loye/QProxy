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
            //acceptor.Accept(localStream);

            string host = "www.baidu.com";
            int port = 80;
            bool SSL = false;

            bool byProxy = this.Proxy != null;
            IPEndPoint endPoint = byProxy ? this.Proxy : new IPEndPoint(DnsHelper.GetHostAddress(host), port);
            Stream remoteStream = SSL ? new HttpsStream(endPoint, host, port, byProxy) : new HttpStream(endPoint);

            if (SSL)
            {
                var res = new Http.HttpResponseHeader(200, Http.HttpStatus.Connection_Established);
                byte[] resBin = res.ToBinary();
                localStream.Write(resBin, 0, resBin.Length);
            }

            byte[] buffer = new byte[10000];
            int len = localStream.Read(buffer, 0, 10000);
            remoteStream.Write(buffer, 0, len);

            len = remoteStream.Read(buffer, 0, 10000);
            localStream.Write(buffer, 0, len);


        }
    }

}
