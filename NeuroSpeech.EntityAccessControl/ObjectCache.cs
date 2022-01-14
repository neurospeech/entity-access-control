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

        public static TV StaticCacheGetOrCreate<T2, TV>(this Type primary, T2 secondary, Func<TV> factory)
        {

            var key = string.Concat(primary.FullName, secondary.ToString());
            return Cache<string, TV>.CacheDictionary.GetOrAdd(key, (x) => factory());
        }

    }
}
