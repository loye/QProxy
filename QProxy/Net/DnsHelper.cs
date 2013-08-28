using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Q.Configuration;

namespace Q.Net
{
    public static class DnsHelper
    {
        private static readonly Regex HOSTS_REGEX = new Regex(@"(?<ip>\d+\.\d+\.\d+\.\d+)[ \t]+(?<host>[^ \t\r\n]+)", RegexOptions.Compiled);

        private static Dictionary<string, IPAddress> hosts = new Dictionary<string, IPAddress>();

        static DnsHelper()
        {
            foreach (var item in ConfigurationManager.Current.dnshelper.dnss)
            {
                IPAddress ip;
                if (!String.IsNullOrWhiteSpace(item.host) && IPAddress.TryParse(item.ip, out ip))
                {
                    hosts.Add(item.host, ip);
                }
            }
        }

        public static IPAddress GetHostAddress(string host)
        {
            IPAddress address;
            if (!TryGetHostAddress(host, out address))
            {
                throw new Exception(String.Format("DNS lookup failed for host: {0}.", host));
            }
            return address;
        }

        public static bool TryGetHostAddress(string host, out IPAddress address)
        {
            address = null;

            if (hosts != null && hosts.ContainsKey(host))
            {
                address = hosts[host];
                return true;
            }

            try
            {
                address = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                return true;
            }
            catch { }

            return false;
        }
    }
}
