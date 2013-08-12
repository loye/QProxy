using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Q;
using Q.Net;
using Q.Proxy;
using Q.Proxy.Net.Http;

namespace CMDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            //DateTime time1 = DateTime.Now;
            //for (int i = 0; i < 1000000; i++)
            //{
            //}
            //DateTime time2 = DateTime.Now;
            //Console.WriteLine(time2 - time1);

            new Listener("127.0.0.1", 2000, false).Start();


            //TestHttpTunnelStream();










            while (true)
            {
                //TestHttpStream();
                var key = Console.ReadKey().Key;
            }
        }

        private static void TestHttpTunnelStream()
        {
            string source = @"GET http://www.baidu.com/ HTTP/1.1
Host: www.baidu.com

";
            var bin = ASCIIEncoding.ASCII.GetBytes(source);

            using (var tunnel = new HttpTunnelStream("http://localhost:1008/", "www.baidu.com", 80))
            {
                tunnel.Write(bin, 0, bin.Length);
                tunnel.Flush();

                Task t2 = Task.Run(() =>
                {
                    byte[] buffer = new byte[4096];
                    var len = tunnel.Read(buffer, 0, 4096);
                    while (len > 0)
                    {
                        Console.Write(ASCIIEncoding.ASCII.GetString(buffer, 0, len));
                        len = tunnel.Read(buffer, 0, 4096);
                    }
                });

                System.Threading.Thread.Sleep(1000);
            }


        }

        private static void TestHttpTunnel()
        {
            //            var s = new HttpTunnelStream(new Uri("http://localhost:1008/gate"), "www.baidu.com", 80, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
            string source = @"GET http://www.baidu.com/ HTTP/1.1
Host: www.baidu.com
Connection: close

";
            var bin = ASCIIEncoding.ASCII.GetBytes(source);
            var binc = bin.ToChunked();

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1008);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);

            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, "http://localhost:1008/", "localhost", 1008);
            httpHeader[HttpHeaderCustomKey.Host] = "www.baidu.com";
            httpHeader[HttpHeaderCustomKey.Port] = 80;
            httpHeader[HttpHeaderCustomKey.Type] = "HttpTunnel";
            httpHeader[HttpHeaderKey.Transfer_Encoding] = "chunked";


            Task t1 = Task.Run(() =>
            {
                stream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);

                stream.Write(binc, 0, binc.Length);
                System.Threading.Thread.Sleep(3000);
                stream.Write(new byte[0].ToChunked(), 0, 3);

            });
            Task t2 = Task.Run(() =>
            {
                byte[] buffer = new byte[4096];
                var len = stream.Read(buffer, 0, 4096);
                while (len > 0)
                {
                    Console.WriteLine(ASCIIEncoding.ASCII.GetString(buffer, 0, len));
                    len = stream.Read(buffer, 0, 4096);
                }
            });
            Task.WaitAll(t1, t2);
        }
    }
}
