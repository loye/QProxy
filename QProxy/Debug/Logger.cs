using System;
using System.Net.Sockets;
using System.Linq;
using Q.Configuration;

namespace Q.Proxy.Debug
{
    public abstract class Logger
    {
        private static Logger m_logger;
        private static readonly object sync = new object();

        public static Logger Current
        {
            get
            {
                if (m_logger == null)
                {
                    lock (sync)
                    {
                        if (m_logger == null)
                        {
                            if (ConfigurationManager.Current.logger == null || ConfigurationManager.Current.logger.Length == 0)
                            {
                                m_logger = new DefaultLogger();
                            }
                            else if (ConfigurationManager.Current.logger.Length == 1)
                            {
                                m_logger = CreateLogger(ConfigurationManager.Current.logger[0]);
                            }
                            else
                            {
                                m_logger = new MultiLogger(ConfigurationManager.Current.logger.Select(c => CreateLogger(c)));
                            }
                        }
                    }
                }
                return m_logger;
            }
            set
            {
                m_logger = value;
            }
        }

        public LogLevel Level { get; set; }

        public Logger(LogLevel level = LogLevel.Error)
        {
            this.Level = level;
        }

        public abstract void Message(string message, LogLevel level);

        public virtual void PublishException(Exception ex, string message = null)
        {
            LogLevel level = ex is SocketException ? LogLevel.Warnning : LogLevel.Error;
            message = String.IsNullOrEmpty(message) ? null : message + "\r\n";
            string exMessage = string.Format("{0}\r\n{1}\r\n{2}", ex.Message, ex.Source, ex.StackTrace);
            this.Message(string.Format("{0}{1}\r\n{2}\r\n", message, ex.GetType(), exMessage), level);
            // Publish inner exception
            if (ex.InnerException != null)
            {
                this.PublishException(ex.InnerException, "Inner Exception:");
            }
        }

        public void Error(string message)
        {
            this.Message(message, LogLevel.Error);
        }

        public void Warnning(string message)
        {
            this.Message(message, LogLevel.Warnning);
        }

        public void Info(string message)
        {
            this.Message(message, LogLevel.Info);
        }

        public void Debug(string message)
        {
            this.Message(message, LogLevel.Debug);
        }

        private static Logger CreateLogger(loggerAdd loggerConfig)
        {
            LogLevel level;
            switch (loggerConfig.type)
            {
                case loggerType.console:
                    return new ConsoleLogger(Enum.TryParse(loggerConfig.level.ToString(), true, out level) ? level : LogLevel.Error);
                default:
                    return new DefaultLogger();
            }
        }
    }


    public class DefaultLogger : Logger
    {
        public override void Message(string message, LogLevel level)
        {
        }
    }

    public enum LogLevel
    {
        Error = 1,
        Warnning = 2,
        Info = 3,
        Debug = 4,
    }
}
