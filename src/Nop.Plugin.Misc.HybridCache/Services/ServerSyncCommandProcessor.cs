using BAS.Nop.Plugin.Misc.HybridCache.Common;
using Nop.Services.Logging;

namespace BAS.Nop.Plugin.Misc.HybridCache.Services
{
    public interface IServerSyncCommandProcessor
    {
        void Process(ServerSyncCommandData command);
        void Process(string command, string[] data);
    }

    public class ServerSyncCommandProcessor : IServerSyncCommandProcessor
    {
        private readonly ICacheStatLogger _cacheStatLogger;
        private readonly ILogger _logger;

        public ServerSyncCommandProcessor(ICacheStatLogger cacheStatLogger, ILogger logger)
        {
            _cacheStatLogger = cacheStatLogger;
            _logger = logger;
        }

        public void Process(ServerSyncCommandData command) 
        {
            Process(command.Command, command.Data);
        }
        public void Process(string command, string[] data)
        {
            switch (command)
            {
                case (BusCommands.SHOW_CACHE_STATS):
                    var keyToSearch = data != null && data.Length > 0 ? data[0] : null;
                    _cacheStatLogger.LogStats(keyToSearch);
                    break;
                default:
                    _logger.Warning($"Server Sync Command is not defined: {command}");
                    break;
            }
        }
    }

    public class ServerSyncCommandData
    {
        public string Command { get; set; }
        public string[] Data { get; set; }
    }
}
