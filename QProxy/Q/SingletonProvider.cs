
namespace Q
{
    public static class SingletonProvider<T> where T : new()
    {
        private static T m_instance;
        private static readonly object sync = new object();

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (sync)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new T();
                        }
                    }
                }
                return m_instance;
            }
        }
    }
}
