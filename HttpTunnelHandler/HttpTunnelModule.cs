using System;
using System.IO;
using System.Web;

namespace Q.Net.Web
{
    public class HttpTunnelModule : IHttpModule
    {
        /// <summary>
        /// You will need to configure this module in the Web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpModule Members

        public void Dispose()
        {
            //clean-up code here.
        }

        public void Init(HttpApplication context)
        {
            // Below is an example of how you can handle LogRequest event and provide 
            // custom logging implementation for it
            context.LogRequest += new EventHandler(OnLogRequest);

            //var request = context.Request;
            //var response = context.Response;

            //using (Stream inputStream = request.InputStream)
            //using (Stream outputStream = response.OutputStream)
            //{
            //    Transfer(inputStream, outputStream, response.Flush);
            //}

        }

        private void Transfer(Stream src, Stream dest, Action action = null)
        {
            byte[] buffer = new byte[4096];
            for (int len = src.Read(buffer, 0, buffer.Length); len > 0; len = src.Read(buffer, 0, buffer.Length))
            {
                dest.Write(buffer, 0, len);
                if (action != null) action();
            }
        }

        #endregion

        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }
    }
}
