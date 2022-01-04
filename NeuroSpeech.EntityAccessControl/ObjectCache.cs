using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{

    public static class ConcurrentCache
    {
        class Cache<T, TV>
        {
            internal static ConcurrentDictionary<T, TV> CacheDictionary = new ConcurrentDictionary<T, TV>();
        }

        public static TV StaticCacheGetOrCreate<T,TV>(this T key, Func<T,TV> factory)
        {
            return Cache<T, TV>.CacheDictionary.GetOrAdd(key, factory);
        }

        public static TV StaticCacheGetOrCreate<T, T2, TV>(this T primary, T2 secondary, Func<T, TV> factory)
        {
            return Cache<(T, T2), TV>.CacheDictionary.GetOrAdd((primary, secondary), (x) => factory(x.Item1));
        }

    }
}
