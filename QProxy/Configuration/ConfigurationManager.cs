using System.IO;
using System.Xml.Serialization;

namespace Q.Configuration
{
    public static class ConfigurationManager
    {
        private const string CONFIG_FILE = @".\config.xml";

        private static configuration m_config;

        public static configuration Current
        {
            get
            {
                if (m_config == null)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(configuration));
                    if (File.Exists(CONFIG_FILE))
                    {
                        using (StreamReader reader = new StreamReader(CONFIG_FILE))
                        {
                            m_config = serializer.Deserialize(reader) as configuration;
                        }
                    }
                    else
                    {
                        m_config = ConfigurationManager.Default;
                        using (StreamWriter writer = new StreamWriter(CONFIG_FILE))
                        {
                            serializer.Serialize(writer, m_config);
                        }
                    }
                }
                return m_config;
            }
        }

        public static configuration Default
        {
            get
            {
                return new configuration()
                {
                    listeners = new listener[] { new listener() { host = "0.0.0.0", port = 1080, type = proxyType.socks, tunnel = new tunnel() { type = tunnelType.local } } },
                    cahelper = new cahelper() { makecert = new makecert() { path = @".\makecert.exe" } },
                    dnshelper = new dnshelper() { dnss = new dnssAdd[] { new dnssAdd() { host = "localhost", ip = "127.0.0.1" } } }
                };
            }
        }
    }
}
