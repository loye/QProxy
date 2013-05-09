using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Q.Proxy;

namespace CMDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            new Listener("127.0.0.1", 1000, true).Start();
            //new Listener("127.0.0.1", 1000, "127.0.0.1", 8888, false).Start();

            //            var s = new HttpPackageStream(new Uri("https://pxy.apphb.com/miner"), "www.baidu.com", 80, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));

            //            string source = @"GET http://www.baidu.com/ HTTP/1.1
            //Host: www.baidu.com
            //Connection: keep-alive
            //Cache-Control: max-age=0
            //Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
            //User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.43 Safari/537.31
            //DNT: 1
            //Accept-Encoding: gzip,deflate,sdch
            //Accept-Language: en-US,zh-CN;q=0.8,en;q=0.6
            //Accept-Charset: GBK,utf-8;q=0.7,*;q=0.3
            //Cookie: BAIDUID=7DFEF0F3AC1F50009812F837497B8C34:FG=1; H_PS_PSSID=1435_1944_1788_2209
            //
            //";
            //            var bin = ASCIIEncoding.ASCII.GetBytes(source);
            //            s.Write(bin, 0, bin.Length);

            Console.WriteLine("Started");
            var key = Console.ReadKey().Key;
        }
    }
}
