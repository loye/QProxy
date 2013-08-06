using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Q.Proxy;
using System.IO;
using Q.Net;
using Q.Proxy.Net.Http;
using System.Net.Sockets;

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

            //new Listener("127.0.0.1", 2000, false).Start();



            TestHttpTunnelStream();










            Console.WriteLine("Started");
            while (true)
            {
                //TestHttpStream();
                var key = Console.ReadKey().Key;
            }
        }

        private static void TestHttpTunnelStream()
        {
            //            var s = new HttpTunnelStream(new Uri("http://localhost:1008/gate"), "www.baidu.com", 80, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
            string source = @"GET http://www.baidu.com HTTP/1.1
Host: www.baidu.com

";
            var bin = ASCIIEncoding.ASCII.GetBytes(source);
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1008);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            Stream stream = new NetworkStream(socket, true);

            var httpHeader = new Q.Net.HttpRequestHeader(HttpMethod.POST, "http://localhost:1008/gate", "localhost", 1008);
            httpHeader[HttpHeaderCustomKey.Host] = "www.baidu.com";
            httpHeader[HttpHeaderCustomKey.Port] = 80;
            //httpHeader["Content-Length"] = bin.Length.ToString();
            httpHeader[HttpHeaderKey.Transfer_Encoding] = "chunked";


            Task t1 = Task.Run(() =>
            {
                stream.Write(httpHeader.ToBinary(), 0, httpHeader.Length);

                //System.Threading.Thread.Sleep(3000);
                stream.Write(ASCIIEncoding.ASCII.GetBytes("3a\r\n"), 0, 4);
                stream.Write(bin, 0, bin.Length);
                stream.Write(ASCIIEncoding.ASCII.GetBytes("\r\n"), 0, 2);

                stream.Write(ASCIIEncoding.ASCII.GetBytes("3a\r\n"), 0, 4);
                stream.Write(bin, 0, bin.Length);
                stream.Write(ASCIIEncoding.ASCII.GetBytes("\r\n"), 0, 2);

                System.Threading.Thread.Sleep(10000);
                stream.Write(ASCIIEncoding.ASCII.GetBytes("0\r\n\r\n"), 0, 5);
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

            /*
            var r = HttpWebRequest.CreateHttp("http://localhost:1008/gate");
            r.Method = "POST";
            //r.Proxy = new WebProxy("http://127.0.0.1:8888");
            r.Headers[HttpHeaderCustomKey.Host] = "www.baidu.com";
            r.Headers[HttpHeaderCustomKey.Port] = "80";
            r.SendChunked = true;
            r.KeepAlive = true;
            using (Stream rs = r.GetRequestStream())
            {
                    rs.Write(bin, 0, bin.Length);
                using (Stream ps = r.GetResponse().GetResponseStream())
                {
                    Task t1 = Task.Run(() =>
                    {
                        rs.Write(bin, 0, bin.Length);
                        System.Threading.Thread.Sleep(3000);
                        rs.Write(bin, 0, bin.Length);
                    });
                    Task t2 = Task.Run(() =>
                    {
                        byte[] buffer = new byte[4096];
                        var len = ps.Read(buffer, 0, 4096);
                        while (len > 0)
                        {
                            Console.WriteLine(ASCIIEncoding.ASCII.GetString(buffer, 0, len));
                            len = ps.Read(buffer, 0, 4096);
                        }
                    });
                    Task.WaitAll(t1, t2);
                }
            }
            */




        }

        private static void Test1()
        {
            string source =
@"POST http://localhost:1008/httpstream HTTP/1.1
HOST: localhost:1008

GET http://www.baidu.com HTTP/1.1
Host: www.baidu.com

";
            var bin = ASCIIEncoding.ASCII.GetBytes(source);



        }

        private static byte[] GetChunked(byte[] src)
        {
            int len = src.Length;
            List<char> la = new List<char>();
            for (int i = len; i > 0; i = len / 16)
            {
                la.Insert(0, (char)(i % 16 + 0x30));
            }
            var dest = new byte[len + la.Count + 4];
            for (int i = 0; i < la.Count; i++)
            {
                dest[i] = (byte)la[i];
            }
            dest[la.Count] = (byte)'\r';
            dest[la.Count + 1] = (byte)'\n';
            Array.Copy(src, 0, dest, la.Count + 2, len);
            dest[len + la.Count + 2] = (byte)'\r';
            dest[len + la.Count + 3] = (byte)'\n';
            return dest;
        }
    }
}
