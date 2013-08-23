using System;
using System.Net.Sockets;

namespace Q.Proxy.Debug
{
    public class ConsoleLogger : Logger
    {
        private static readonly object consoleLocker = new object();

        public override void Message(string message, int level)
        {
            if (level >= this.Level)
            {
                lock (consoleLocker)
                {
                    Console.ForegroundColor = level == 1 ? ConsoleColor.Red : (level == 2 ? ConsoleColor.Yellow : ConsoleColor.Gray);
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }
    }
}
