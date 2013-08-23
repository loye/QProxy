using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Q;
using Q.Net;
using Q.Proxy;

namespace CMDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            new QProxy().Start();

            while (true)
            {
                Thread.Sleep(int.MaxValue);
            }
        }
    }
}
