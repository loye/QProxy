using System.Collections.Generic;
using Q.Configuration;

namespace Q.Proxy
{
    public class QProxy
    {
        private List<Listener> Listeners = new List<Listener>();

        public QProxy()
        {
        }

        public void Start()
        {
            foreach (var item in ConfigurationManager.Current.listeners)
            {
                Repeater repeater;
                switch (item.type)
                {
                    case proxyType.socks:
                        repeater = new SocksRepeater(item);
                        break;
                    case proxyType.http:
                        repeater = new HttpRepeater(item);
                        break;
                    default:
                        repeater = new HttpRepeater(item);
                        break;
                }
                this.Listeners.Add(new Listener(item.host, item.port, repeater));
            }

            foreach (var item in Listeners)
            {
                item.Start();
            }

        }

        public void Stop()
        {
            foreach (var item in Listeners)
            {
                item.Stop();
            }
        }
    }
}
