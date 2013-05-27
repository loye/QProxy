using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Q.Net
{
    public abstract class HttpHeader : IEnumerable<HttpHeaderItem>
    {
        private static readonly Regex REGEX_HEADER = new Regex(
           @"(?:^(?<request>(?<method>[A-Z]+)\s(?<url>(?:(?<schema>\w+)\://)?(?<host>[^/: ]+)?(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|^(?<response>(?<version>HTTP\S+)\s(?<statusCode>\d+)\s(?<status>.*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
           RegexOptions.Compiled);

        protected string m_rawString;

        private Dictionary<string, HttpHeaderItem> m_headerItems = new Dictionary<string, HttpHeaderItem>();

        public abstract string StartLine { get; }

        public string Version { get; protected set; }

        public int Length
        {
            get
            {
                return m_rawString != null ? m_rawString.Length : this.ToString().Length;
            }
        }

        public int ContentLength
        {
            get
            {
                int length = 0;
                return int.TryParse(this[HttpHeaderKey.Content_Length], out length)
                    ? (length < 0 ? 0 : length)
                    : (this[HttpHeaderKey.Transfer_Encoding] == "chunked" ? -1 : 0);
            }
            set
            {
                m_headerItems[HttpHeaderKey.Content_Length] = new HttpHeaderItem(HttpHeaderKey.Content_Length, value.ToString());
                m_rawString = null;
            }
        }

        public HttpHeaderItem this[string key]
        {
            get
            {
                return m_headerItems.ContainsKey(key) ? m_headerItems[key] : null;
            }
            set
            {
                if (key != null)
                {
                    if (value == null)
                    {
                        m_headerItems.Remove(key);
                    }
                    else
                    {
                        value.Key = key;
                        m_headerItems[key] = value;
                    }
                    m_rawString = null;
                }
            }
        }

        public HttpHeader Append(HttpHeaderItem item)
        {
            if (item.Key != null)
            {
                if (m_headerItems.ContainsKey(item.Key))
                {
                    m_headerItems[item.Key].Add(item);
                }
                else
                {
                    m_headerItems[item.Key] = item;
                }
                m_rawString = null;
            }
            return this;
        }

        public int HeaderItemCount()
        {
            int count = 0;
            foreach (var i in m_headerItems)
            {
                HttpHeaderItem item = i.Value;
                count += item != null && item.Values != null ? item.Values.Length : 0;
            }
            return count;
        }

        public int HeaderItemCount(string key)
        {
            int count = 0;
            if (m_headerItems.ContainsKey(key))
            {
                HttpHeaderItem item = m_headerItems[key];
                count = item != null && item.Values != null ? item.Values.Length : 0;
            }
            return count;
        }

        public static bool TryParse(string source, out HttpHeader httpHeader)
        {
            httpHeader = null;
            Match match = REGEX_HEADER.Match(source);
            if (match.Success)
            {
                if (match.Groups["request"].Success)
                {
                    httpHeader = new HttpRequestHeader(
                        match.Groups["method"].Value,
                        match.Groups["url"].Value,
                        match.Groups["host"].Value,
                        match.Groups["port"].Success
                            ? int.Parse(match.Groups["port"].Value)
                            : (match.Groups["schema"].Success && match.Groups["schema"].Value.ToLower() == "https" ? 443 : 80),
                        match.Groups["version"].Value);
                }
                else if (match.Groups["response"].Success)
                {
                    httpHeader = new HttpResponseHeader(
                        int.Parse(match.Groups["statusCode"].Value),
                        match.Groups["status"].Value,
                        match.Groups["version"].Value);
                }
                if (httpHeader != null)
                {
                    Group keyGroup = match.Groups["key"];
                    Group valueGroup = match.Groups["value"];
                    for (int i = 0, headerCount = keyGroup.Captures.Count; i < headerCount; i++)
                    {
                        string key = keyGroup.Captures[i].Value;
                        string value = valueGroup.Captures[i].Value;
                        httpHeader.Append(new HttpHeaderItem(key, value));
                    }
                    httpHeader.m_rawString = match.Captures[0].Value;
                    return true;
                }
            }
            return false;
        }

        public static bool TryParse(byte[] binary, int startIndex, int length, out HttpHeader httpHeader)
        {
            string source = ASCIIEncoding.ASCII.GetString(binary, startIndex, length);
            return HttpHeader.TryParse(source, out httpHeader);
        }

        public static HttpHeader Parse(string source)
        {
            HttpHeader httpHeader;
            return HttpHeader.TryParse(source, out httpHeader) ? httpHeader : null;
        }

        public static HttpHeader Parse(byte[] binary, int startIndex, int length)
        {
            HttpHeader httpHeader;
            return HttpHeader.TryParse(binary, startIndex, length, out httpHeader) ? httpHeader : null;
        }

        public byte[] ToBinary()
        {
            return ASCIIEncoding.ASCII.GetBytes(this.ToString());
        }

        public override string ToString()
        {
            if (m_rawString == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.StartLine);
                foreach (var item in this)
                {
                    sb.Append(item.ToString());
                }
                sb.Append("\r\n");
                m_rawString = sb.ToString();
            }
            return m_rawString;
        }

        IEnumerator<HttpHeaderItem> IEnumerable<HttpHeaderItem>.GetEnumerator()
        {
            return m_headerItems.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_headerItems.Values.GetEnumerator();
        }
    }
}
