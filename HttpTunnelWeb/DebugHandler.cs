using System;
using System.Text;
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
            response.ContentType = "text/html; charset=utf-8";

            response.Write(GetThreadPoolInfo());
            response.Write(GetHttpTunnelInfo());
        }

        private string GetThreadPoolInfo()
        {
            int minWorkerThreads, minCompletionPortThreads,
               maxWorkerThreads, maxCompletionPortThreads,
               availableWorkerThreads, availableCompletionPortThreads;

            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);

            return new StringBuilder()
                .Append("<div>ThreadPool Information</div>")
                .Append("<table>")
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "MinWorkerThreads", minWorkerThreads.ToString())
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "MinCompletionPortThreads", minCompletionPortThreads.ToString())
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "MaxWorkerThreads", maxWorkerThreads.ToString())
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "MaxCompletionPortThreads", maxCompletionPortThreads.ToString())
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "AvailableWorkerThreads", availableWorkerThreads.ToString())
                .AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", "AvailableCompletionPortThreads", availableCompletionPortThreads.ToString())
                .Append("</table>")
                .Append("<br />")
                .ToString();
        }

        private string GetHttpTunnelInfo()
        {
            return HttpTunnelNode.Instance.ToString();
        }

        #endregion
    }
}
