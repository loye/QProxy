using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;

namespace Q.Net.Web
{
    public class HttpTunnelHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            switch (context.Request.HttpMethod)
            {
                case "POST":
                    POST(context);
                    break;
                case "GET":
                    GET(context);
                    break;
                default:
                    break;
            }
        }

        #endregion

        private void GET(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Write("It's working!");
            response.End();
        }

        private void POST(HttpContext context)
        {
        }
    }
}
