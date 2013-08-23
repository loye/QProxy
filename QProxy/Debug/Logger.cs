using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Proxy.Debug
{
    public abstract class Logger
    {
        public int Level { get; set; }

        public Logger(int level = 0)
        {
            this.Level = level;
        }

        public abstract void Message(string message, int level);

        public abstract void PublishException(Exception ex, string message = null);

        protected void Error(string message)
        {
            this.Message(message, 1);
        }

        protected void Warnning(string message)
        {
            this.Message(message, 2);
        }

        protected void Info(string message)
        {
            this.Message(message, 3);
        }
    }
}
