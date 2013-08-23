using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Proxy.Debug
{
    public class MultiLogger : Logger
    {
        public List<Logger> Loggers { get; set; }

        public MultiLogger(List<Logger> loggers)
        {
            if (loggers == null || loggers.Count() == 0)
            {
                throw new ArgumentNullException("loggers", "loggers can't be null or empty list");
            }
            this.Loggers = loggers;
        }

        public override void Message(string message, int level)
        {
            foreach (var logger in Loggers)
            {
                if (logger != null)
                {
                    logger.Message(message, level);
                }
            }
        }

        public override void PublishException(Exception ex, string message = null)
        {
            foreach (var logger in Loggers)
            {
                if (logger != null)
                {
                    logger.PublishException(ex, message);
                }
            }
        }
    }
}
