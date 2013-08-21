﻿using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Net.Web
{
    public class HttpTunnelNode
    {
        private static ConcurrentDictionary<string, Tunnel> tunnelPool = new ConcurrentDictionary<string, Tunnel>();

        private static HttpTunnelNode m_instance;

        private static object locker = new object();

        private HttpTunnelNode() { }

        public static HttpTunnelNode Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new HttpTunnelNode();
                        }
                    }
                }
                return m_instance;
            }
        }

        public void Connect(string id, string host, IPEndPoint endPoint)
        {
            tunnelPool.TryAdd(id, new Tunnel(id, host, endPoint));
        }

        public void Write(string id, byte[] buffer, int offset, int count)
        {
            tunnelPool[id].Stream.Write(buffer, offset, count);
        }

        public int Read(string id, byte[] buffer, int offset, int count)
        {
            return tunnelPool[id].Stream.Read(buffer, offset, count);
        }

        public void Close(string id)
        {
            Tunnel tunnel;
            if (tunnelPool.TryRemove(id, out tunnel))
            {
                tunnel.Dispose();
            }
        }

        public void StartCleaner(int cycle)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        int timeout;
                        TimeSpan timeoutSpan = new TimeSpan(0, 0, Int32.TryParse(ConfigurationManager.AppSettings["TunnelTimeout"], out timeout) ? timeout : 900);
                        Thread.Sleep(cycle * 1000);
                        foreach (var item in tunnelPool)
                        {
                            if (DateTime.Now - item.Value.LastActivityTime > timeoutSpan)
                            {
                                this.Close(item.Value.ID);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><td>ID</td><td>Host</td><td>IPEndPoint</td><td>LastActivityTime</td></tr>");
            foreach (var item in tunnelPool)
            {
                count++;
                sb.AppendLine(item.Value.ToString());
            }
            sb.AppendLine("</table>");
            return String.Format("Total Count: {0}<br />{1}", count, count > 0 ? sb.ToString() : null);
        }

        private class Tunnel : IDisposable
        {
            public string ID { get; private set; }

            public string Host { get; private set; }

            public IPEndPoint IPEndPoint { get; private set; }

            public DateTime LastActivityTime { get; private set; }

            public Stream Stream
            {
                get
                {
                    this.LastActivityTime = DateTime.Now;
                    return m_stream;
                }
            }

            private Stream m_stream;

            public Tunnel(string id, string host, IPEndPoint endPoint)
            {
                this.ID = id;
                this.Host = host;
                this.IPEndPoint = endPoint;
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(endPoint);
                m_stream = new NetworkStream(socket, true);
                this.LastActivityTime = DateTime.Now;
            }

            public override string ToString()
            {
                return String.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>", this.ID, this.Host, this.IPEndPoint, this.LastActivityTime);
            }

            public void Dispose()
            {
                this.Stream.Dispose();
            }
        }
    }
}