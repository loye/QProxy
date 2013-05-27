using System;
using System.Threading;
using System.Web;

namespace Q.Net.Web
{
    public class EndPointHandler : IHttpHandler
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
                case "Post":
                    POST(context);
                    break;
                case "GET":
                    GET(context);
                    break;
                default:
                    break;
            }
        }

        private void GET(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Headers["Connection"] = "close";
            response.Write("ABC");
            response.Flush();
            response.Write("DEF");
            response.End();
        }

        private void POST(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.Headers["Connection"] = "close";






            response.End();
        }

        #endregion
    }
}
