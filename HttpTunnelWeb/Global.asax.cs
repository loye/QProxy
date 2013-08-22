using System;
using System.Configuration;
using System.Threading;

namespace Q.Net.Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ThreadPool.SetMinThreads(1024, 1000);
            int cycle;
            HttpTunnelNode.Instance.StartCleaner(Int32.TryParse(ConfigurationManager.AppSettings["TunnelTimeout"], out cycle) ? cycle : 600);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}