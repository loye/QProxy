using System;
using System.Net.Sockets;

namespace Q.Proxy.Debug
{
    public class ConsoleLogger : Logger
    {
        private static readonly object consoleLocker = new object();

        public ConsoleLogger(LogLevel level = LogLevel.Error)
            : base(level)
        {
        }

        public override void Message(string message, LogLevel level)
        {
            if (level <= this.Level)
            {
                lock (consoleLocker)
                {
                    Console.ForegroundColor = level == LogLevel.Error ? ConsoleColor.Red : (level == LogLevel.Warnning ? ConsoleColor.Yellow : ConsoleColor.Gray);
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }
    }
}
