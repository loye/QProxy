using System;
using System.Text;

namespace QProxy
{
    public class HttpHeaderItem
    {
        private string[] m_Items;

        public string Key { get; set; }

        public string Value
        {
            get
            {
                return (m_Items == null || m_Items.Length == 0) ? null : m_Items[0];
            }
            set
            {
                m_Items = (m_Items == null || m_Items.Length != 1) ? new string[1] : m_Items;
                m_Items[0] = value;
            }
        }

        public string[] Values
        {
            get
            {
                return m_Items;
            }
        }

        public HttpHeaderItem(string key) : this(key, default(string)) { }

        public HttpHeaderItem(string key, string value) : this(key, value == null ? null : new string[] { value }) { }

        public HttpHeaderItem(string key, HttpHeaderItem item) : this(key, item == null ? null : item.Values) { }

        public HttpHeaderItem(string key, string[] values)
        {
            this.Key = key;
            m_Items = values;
        }

        public HttpHeaderItem Add(string value)
        {
            return Add(new string[] { value });
        }

        public HttpHeaderItem Add(HttpHeaderItem item)
        {
            return item == null ? this : Add(item.Values);
        }

        public HttpHeaderItem Add(string[] values)
        {
            if (values != null && values.Length > 0)
            {
                string[] newItems;
                if (m_Items == null || m_Items.Length == 0)
                {
                    newItems = new string[values.Length];
                }
                else
                {
                    newItems = new string[m_Items.Length + values.Length];
                    Array.Copy(m_Items, newItems, m_Items.Length);
                }
                Array.Copy(values, 0, newItems, m_Items.Length, values.Length);
                m_Items = newItems;
            }
            return this;
        }

        public override string ToString()
        {
            if (m_Items == null || m_Items.Length == 0)
            {
                return null;
            }
            if (m_Items.Length == 1)
            {
                return String.Format("{0}: {1}\r\n", Key, m_Items[0]);
            }
            StringBuilder sb = new StringBuilder();
            foreach (var item in m_Items)
            {
                sb.AppendFormat("{0}: {1}\r\n", Key, item);
            }
            return sb.ToString();
        }

        public static implicit operator string(HttpHeaderItem headerItem)
        {
            return headerItem == null ? null : headerItem.Value;
        }

        public static implicit operator HttpHeaderItem(string value)
        {
            return new HttpHeaderItem(null, value);
        }
    }
}
