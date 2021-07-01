using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyCaching.Core;
using Nop.Core.Caching;

namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public interface ICacheStatMonitor
    {
        Dictionary<string, string> GetAllCacheStats(string keyToSearch);
    }

    public class CacheStatMonitor : ICacheStatMonitor
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CacheStatMonitor(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public Dictionary<string, string> GetAllCacheStats(string keyToSearch)
        {
            var stats = new Dictionary<string, string>(0);
            var memCacheMgr = _staticCacheManager as BasMemoryCacheManager;
            if (memCacheMgr != null)
            {
                var hybridProviderField = memCacheMgr.GetType().GetField("_provider", BindingFlags.NonPublic | BindingFlags.Instance);
                var hybridProvider = hybridProviderField.GetValue(memCacheMgr);
                //_localCache + _distributedCache are the inner fields available
                var providerField = hybridProvider.GetType().GetField("_localCache", BindingFlags.NonPublic | BindingFlags.Instance);
                var provider = providerField.GetValue(hybridProvider);
                if (provider != null)
                {
                    var cacheStatsField = provider.GetType().GetProperty("CacheStats", BindingFlags.Public | BindingFlags.Instance);
                    if (cacheStatsField != null)
                    {
                        var cacheStats = cacheStatsField.GetValue(provider) as CacheStats;
                        if (cacheStats != null)
                        {
                            var hits = cacheStats.GetStatistic(StatsType.Hit);
                            var misses = cacheStats.GetStatistic(StatsType.Missed);
                            stats.Add("Hits", hits.ToString("N0"));
                            stats.Add("Misses", misses.ToString("N0"));
                        }
                    }
                    var cacheField = provider.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (cacheField != null)
                    {
                        var cache = cacheField.GetValue(provider);
                        if (cache != null)
                        {
                            var memoryField = cache.GetType().GetField("_memory", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (memoryField != null)
                            {
                                var memory = memoryField.GetValue(cache);
                                if (memory != null)
                                {
                                    var countField = memory.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                                    if (countField != null)
                                    {
                                        var count = (int)countField.GetValue(memory);
                                        stats.Add("Total Keys", count.ToString("N0"));
                                    }
                                    if (!string.IsNullOrEmpty(keyToSearch))
                                    {
                                        if (((IEasyCachingProvider)provider).Exists(keyToSearch))
                                        {
                                            GetKey(keyToSearch, stats, cache);
                                        }
                                        else
                                        {
                                            var keysProp = memory.GetType().GetProperty("Keys", BindingFlags.Public | BindingFlags.Instance);
                                            ICollection<string> allKeys = keysProp.GetValue(memory) as ICollection<string>;
                                            if (allKeys != null && allKeys.Count > 0)
                                            {
                                                var keys = allKeys.Where(k => k.StartsWith(keyToSearch)).ToArray();
                                                if (keys.Length > 0)
                                                {
                                                    stats.Add("Keys Found", keys.Length.ToString("N0"));
                                                    stats.Add("---start---", "---------");
                                                    foreach (var key in keys)
                                                    {
                                                        GetKey(key, stats, cache);
                                                    }
                                                    stats.Add("---end---", "---------");
                                                }
                                                else
                                                {
                                                    stats.Add(keyToSearch, "Not found.");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return stats;
        }

        private static void GetKey(string keyToSearch, Dictionary<string, string> stats, object cache)
        {
            object obj = ((EasyCaching.InMemory.IInMemoryCaching)cache).Get(keyToSearch);
            try
            {
                var val = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                stats.Add(keyToSearch, val);
            }
            catch
            {
                stats.Add(keyToSearch, $"Exists but cannot serialize: {obj}");
            }
        }

    }
}
