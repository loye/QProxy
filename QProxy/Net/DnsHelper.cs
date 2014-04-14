using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Q.Net
{
    public static class DnsHelper
    {
        private static Dictionary<string, IPAddress> hosts = new Dictionary<string, IPAddress>();

        public static void AppendHosts(Dictionary<string, IPAddress> hosts)
        {
            foreach (var item in hosts)
            {
                if (item.Key != null && item.Value != null)
                {
                    DnsHelper.hosts[item.Key] = item.Value;
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
