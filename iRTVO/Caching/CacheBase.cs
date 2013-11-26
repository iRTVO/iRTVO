using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Caching
{
    public class CacheBase<T>
    {
        Dictionary<string, T> cache = new Dictionary<string, T>();
        Dictionary<string, int> cachehits = new Dictionary<string, int>();

        public CacheBase()
        {

        }

        public string Statistics
        {
            get
            {

                StringBuilder s = new StringBuilder();
                s.AppendLine("Caching Statistics:");
                var cSort = (from c in cachehits select c).OrderByDescending(c => c.Value);
                foreach (var p in cSort)
                    s.AppendLine(String.Format("{0} {1}", p.Value, p.Key));
                return s.ToString();

            }
        }

        public void Add(string key, T value)
        {
            key = key.ToUpperInvariant();
            cache[key] = value;
            cachehits[key] = 1;
        }

        public T Get(string key)
        {
            key = key.ToUpperInvariant();
            if (cache.ContainsKey(key))
            {
                cachehits[key] = cachehits[key]+1;
                return cache[key];
            }
            return default(T);
        }

        public bool Contains(string key)
        {
            return cache.ContainsKey( key.ToUpperInvariant() );
        }

        public int CleanUp()
        {
            return CleanUp(10);
        }

        public int CleanUp(int threshold)
        {
            int removed = 0;
            foreach(string key in cache.Keys)
            {
                if (cachehits[key] < threshold)
                {
                    cache.Remove(key);
                    cachehits.Remove(key);
                    removed++;
                }
            }
            return removed;
        }
    }
}
