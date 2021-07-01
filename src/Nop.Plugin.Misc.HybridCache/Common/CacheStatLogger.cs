using System.Text;
using Nop.Services.Logging;
using Nop.Core.Domain.Logging;

namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public interface ICacheStatLogger
    {
        void LogStats(string keyToSearch);
    }

    public class CacheStatLogger : ICacheStatLogger
    {
        private readonly ICacheStatMonitor _cacheStatMonitor;
        private readonly IThreadMonitor _threadMonitor;
        private readonly ILogger _logger;

        public CacheStatLogger(ICacheStatMonitor cacheStatMonitor, IThreadMonitor threadMonitor, ILogger logger)
        {
            _cacheStatMonitor = cacheStatMonitor;
            _threadMonitor = threadMonitor;
            _logger = logger;
        }
        public void LogStats(string keyToSearch)
        {
            var sb = new StringBuilder();
            var cacheStats = _cacheStatMonitor.GetAllCacheStats(keyToSearch);
            foreach (var key in cacheStats.Keys)
            {
                sb.AppendLine($"{key}: {cacheStats[key]}");
            }
            sb.AppendLine($"Thread Info: {_threadMonitor.GetThreadPoolStatsFormatted()}");
            _logger.InsertLog(LogLevel.Information, $"Cache Stats: {System.Environment.MachineName}", sb.ToString(), null);
        }
    }
}
