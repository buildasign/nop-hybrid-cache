using System;
using Nop.Core.Configuration;
using Nop.Core.Redis;
using StackExchange.Redis;

namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public interface IBusAccessor
    {
        void Publish(string topic, string message);
        void Subcribe(string topic, Action<RedisChannel, RedisValue> handler);
    }

    public class BusAccessor : IBusAccessor
    {
        private readonly IRedisConnectionWrapper _redisConnectionWrapper;
        private readonly NopConfig _config;

        public BusAccessor(IRedisConnectionWrapper redisConnectionWrapper, NopConfig config)
        {
            _redisConnectionWrapper = redisConnectionWrapper;
            _config = config;
        }

        public void Subcribe(string topic, System.Action<RedisChannel, RedisValue> handler)
        {
            var db = GetRedisDb();
            db.Multiplexer.GetSubscriber().Subscribe(new RedisChannel(topic, RedisChannel.PatternMode.Auto), handler);
        }

        public void Publish(string topic, string message)
        {
            var db = GetRedisDb();
            db.Publish(new RedisChannel(topic, RedisChannel.PatternMode.Auto), message);
        }

        private IDatabase GetRedisDb()
        {
            return _redisConnectionWrapper.GetDatabase(_config.RedisDatabaseId ?? 0 + 2);
        }
    }
}
