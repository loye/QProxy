﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Q.Proxy;
using System.IO;
using Q.Net;

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

            //new Listener("127.0.0.1", 1000, true).Start();

            //new Listener("127.0.0.1", 1000, true).Start();
            TestHttpStream();
            Console.WriteLine("Started");
            while (true)
            {
                var key = Console.ReadKey().Key;
            }
        }

        private static void TestHttpStream()
        {
            var s = new HttpStream(new Uri("http://vmnet1.com:1008/httpstream"), "www.baidu.com", 80, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
            string source = @"GET http://vmnet1.com:1008/httpstream HTTP/1.1
Host: vmnet1.com:1008
          
";
            var bin = ASCIIEncoding.ASCII.GetBytes(source);

            s.Write(bin, 0, bin.Length);

            
        }
    }
}
