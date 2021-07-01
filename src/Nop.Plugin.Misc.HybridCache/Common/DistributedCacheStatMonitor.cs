using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyCaching.Core;
using EasyCaching.Redis;
using Nop.Core.Caching;
using StackExchange.Redis;

namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public interface IDistributedCacheStatMonitor : ICacheStatMonitor { }

    public class DistributedCacheStatMonitor : IDistributedCacheStatMonitor
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public DistributedCacheStatMonitor(IStaticCacheManager staticCacheManager)
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
                var distributedProviderField = hybridProvider.GetType().GetField("_distributedCache", BindingFlags.NonPublic | BindingFlags.Instance);
                var distributedProvider = distributedProviderField.GetValue(hybridProvider) as DefaultRedisCachingProvider;
                if (distributedProvider != null)
                {
                    stats.Add("Distributed Total Keys", distributedProvider.GetCount().ToString("N0"));
                    var cacheStatsField = distributedProvider.GetType().GetProperty("CacheStats", BindingFlags.Public | BindingFlags.Instance);
                    if (cacheStatsField != null)
                    {
                        var cacheStats = cacheStatsField.GetValue(distributedProvider) as CacheStats;
                        if (cacheStats != null)
                        {
                            var hits = cacheStats.GetStatistic(StatsType.Hit);
                            var misses = cacheStats.GetStatistic(StatsType.Missed);
                            stats.Add("Distributed Hits", hits.ToString("N0"));
                            stats.Add("Distributed Misses", misses.ToString("N0"));
                        }
                    }
                    if (!string.IsNullOrEmpty(keyToSearch))
                    {
                        if (distributedProvider.Exists(keyToSearch))
                        {//key match
                            var data = distributedProvider.StringGet(keyToSearch);
                            stats.Add(keyToSearch, data);
                        }
                        else
                        {//search by prefix
                            var cacheField = distributedProvider.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (cacheField != null)
                            {
                                var cache = cacheField.GetValue(distributedProvider) as StackExchange.Redis.IDatabase;
                                if (cache != null)
                                {
                                    var searchByRedisKeysMethod = distributedProvider.GetType().GetMethod("SearchRedisKeys", BindingFlags.NonPublic | BindingFlags.Instance);

                                    var redisKeys = searchByRedisKeysMethod.Invoke(distributedProvider, new[] { keyToSearch }) as RedisKey[];

                                    var values = cache.StringGet(redisKeys).ToArray();
                                    for (int i = 0; i < redisKeys.Length; i++)
                                    {
                                        var cachedValue = values[i];
                                        if (!cachedValue.IsNull)
                                            stats.Add(redisKeys[i], cachedValue);
                                        else
                                            stats.Add(redisKeys[i], string.Empty);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            return stats;
        }
    }
}
