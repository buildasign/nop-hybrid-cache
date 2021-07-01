using System.Threading;
using System.Threading.Tasks;
using BAS.Nop.Plugin.Misc.HybridCache.Common;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Nop.Services.Logging;
using StackExchange.Redis;

namespace BAS.Nop.Plugin.Misc.HybridCache.Services
{
    public class ServerSyncService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBusAccessor _busAccessor;
        private readonly IServerSyncCommandProcessor _serverSyncCommandProcessor;

        public ServerSyncService(ILogger logger, IBusAccessor busAccessor, IServerSyncCommandProcessor serverSyncCommandProcessor)
        {
            _logger = logger;
            _busAccessor = busAccessor;
            _serverSyncCommandProcessor = serverSyncCommandProcessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //subscribe
            _busAccessor.Subcribe(BusConstants.SYNC_TOPIC, OnMessage);
            _logger.Information("Server Sync Subscribed");

            return Task.CompletedTask;
        }

        private void OnMessage(RedisChannel channel, RedisValue val)
        {
            if (channel == BusConstants.SYNC_TOPIC)
            {
                if (val.HasValue)
                {
                    var message = JsonConvert.DeserializeObject(val, typeof(ServerSyncCommandData)) as ServerSyncCommandData;
                    if (message != null)
                    {
                        _serverSyncCommandProcessor.Process(message);
                    }
                }
            }
            //if not meant for us ignore
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
