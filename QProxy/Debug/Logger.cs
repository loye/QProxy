using System;
using System.Net.Sockets;

namespace Q.Proxy.Debug
{
    public abstract class Logger
    {
        private static Logger m_logger;

        public static Logger Instance
        {
            get
            {
                if (m_logger == null)
                {
                    m_logger = new DefaultLogger();
                }
                return m_logger;
            }
            set
            {
                m_logger = value;
            }
        }

        public int Level { get; set; }

        public Logger(int level = 1)
        {
            this.Level = level;
        }

        public abstract void Message(string message, int level);

        public virtual void PublishException(Exception ex, string message = null)
        {
            int level = ex is SocketException ? 2 : 1;
            message = String.IsNullOrEmpty(message) ? null : message + "\r\n";
            string exMessage = string.Format("{0}\r\n{1}\r\n{2}", ex.Message, ex.Source, ex.StackTrace);
            this.Message(string.Format("{0}{1}\r\n{2}\r\n", message, ex.GetType(), exMessage), level);
            // Publish inner exception
            if (ex.InnerException != null)
            {
                this.PublishException(ex.InnerException, "Inner Exception:");
            }
        }

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

    public class DefaultLogger : Logger
    {
        public override void Message(string message, int level)
        {
        }
    }
}
