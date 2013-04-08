using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Q.Proxy;

namespace CMDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            new Listener("127.0.0.1", 1000, "127.0.0.1", 8888).Start();


            while (true)
            {
                var key = Console.ReadKey().Key;
                Console.WriteLine();
            }

        }
    }
}
