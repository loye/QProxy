using System;
using System.Threading;
using System.Web;

namespace Q.Net.Web
{
    public class DebugHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            int minWorkerThreads, minCompletionPortThreads, maxWorkerThreads, maxCompletionPortThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);

            response.Write("<table>");

            response.Write(GetTableLine("MinWorkerThreads", minWorkerThreads.ToString()));
            response.Write(GetTableLine("MinCompletionPortThreads", minCompletionPortThreads.ToString()));
            response.Write(GetTableLine("MaxWorkerThreads", maxWorkerThreads.ToString()));
            response.Write(GetTableLine("MaxCompletionPortThreads", maxCompletionPortThreads.ToString()));

            response.Write("</table>");
        }

        private string GetTableLine(string name, string value)
        {
            return String.Format("<tr><td>{0}</td><td>{1}</td></tr>", name, value);
        }

        #endregion
    }
}
