using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Q.Configuration;
using Q.Proxy.Debug;

namespace Q.Proxy
{
    public class QProxy
    {
        public QProxy()
        {


            Logger.Instance = new ConsoleLogger();
        }

        public void Start()
        {
            foreach (var item in ConfigurationManager.Current.listeners)
            {

            }

            new Listener<SocksRepeater>("127.0.0.1", 2000).Start();

        }

        public void Stop()
        {

        }
    }
}
