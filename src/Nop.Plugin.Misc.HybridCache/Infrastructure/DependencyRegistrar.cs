using Autofac;
using Autofac.Core;
using BAS.Nop.Plugin.Misc.HybridCache.Common;
using BAS.Nop.Plugin.Misc.HybridCache.Services;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Redis;
using Nop.Plugin.Misc.HybridCache.Workers;

namespace BAS.Nop.Plugin.Misc.HybridCache.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => 20;   //ensure this goes after NOP but before other plugins

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //custom logger for EasyCaching
            //TODO: This cannot be used as EasyCaching tries to log things before EF is active. Tackle later.
            //builder.RegisterGeneric(typeof(EasyLogger<>)).As(typeof(Microsoft.Extensions.Logging.ILogger<>)).InstancePerLifetimeScope();
            //static cache manager
            builder.RegisterType<BasMemoryCacheManager>()
                .As<ILocker>()
                .As<IStaticCacheManager>()
                .SingleInstance();
            builder.RegisterType<ThreadMonitor>().As<IThreadMonitor>().InstancePerLifetimeScope();
            builder.RegisterType<CacheStatMonitor>().As<ICacheStatMonitor>().InstancePerLifetimeScope();
            builder.RegisterType<DistributedCacheStatMonitor>().As<IDistributedCacheStatMonitor>().InstancePerLifetimeScope();
            builder.RegisterType<CacheStatLogger>().As<ICacheStatLogger>().InstancePerLifetimeScope();
            builder.RegisterType<BusAccessor>()
                .As<IBusAccessor>()
                .WithParameter("redisConnectionWrapper", new RedisConnectionWrapper(config))
                .WithParameter("config", config)
                .InstancePerLifetimeScope();
            builder.RegisterType<ServerSyncCommandProcessor>().As<IServerSyncCommandProcessor>().InstancePerLifetimeScope();
            builder.RegisterType<BackgroundTaskQueue>().As<IBackgroundTaskQueue>().InstancePerLifetimeScope();
        }
    }
}