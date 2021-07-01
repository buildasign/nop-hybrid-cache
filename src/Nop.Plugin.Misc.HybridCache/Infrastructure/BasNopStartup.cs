using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using BAS.Nop.Plugin.Misc.HybridCache.Services;
using EasyCaching.Bus.Redis;
using EasyCaching.Core;
using EasyCaching.Core.Configurations;
using EasyCaching.HybridCache;
using EasyCaching.InMemory;
using EasyCaching.Redis;
using EasyCaching.Serialization.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Nop.Core.Infrastructure;

namespace BAS.Nop.Plugin.Misc.HybridCache.Infrastructure
{
    public class BasNopStartup : INopStartup
    {
        public int Order => 101; //add after NOP

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var threadSettings = ParseThreadSettings(configuration);
            if (threadSettings.MinIoThreads > 0 && threadSettings.MinWorkerThreads > 0)
            {
                ThreadPool.SetMinThreads(threadSettings.MinWorkerThreads, threadSettings.MinIoThreads);
            }

            var cacheSettings = ParseCacheSettings(configuration);
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IInMemoryCaching));
            if (serviceDescriptor != null)
                services.Remove(serviceDescriptor);

            services.AddEasyCaching(option =>
            {
                Action<JsonSerializerSettings> settings = config =>
                {
                    config.ContractResolver = new CustomResolver(new[] { "Id" });
                    config.PreserveReferencesHandling = PreserveReferencesHandling.None;
                    config.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    config.ConstructorHandling = ConstructorHandling.Default;
                    config.DefaultValueHandling = DefaultValueHandling.Include;
                    config.MetadataPropertyHandling = MetadataPropertyHandling.Default;
                    config.MissingMemberHandling = MissingMemberHandling.Ignore;
                    config.NullValueHandling = NullValueHandling.Include;
                    config.ObjectCreationHandling = ObjectCreationHandling.Auto;
                    config.StringEscapeHandling = StringEscapeHandling.Default;
                    config.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
                    config.TypeNameHandling = TypeNameHandling.None;
                };
                option.WithJson(settings);
                // local
                option.UseInMemory(config =>
                {
                    config.Order = 0;
                    config.DBConfig = new InMemoryCachingOptions
                    {
                        // scan time, default value is 60s
                        ExpirationScanFrequency = 60,
                        // total count of cache items, default value is 10000
                        SizeLimit = cacheSettings.CacheLimit,
                    };
                    // the max random second will be added to cache's expiration, default value is 120
                    config.MaxRdSecond = 120;
                    // whether enable logging, default is false
                    config.EnableLogging = cacheSettings.EnableLogging; //to enable, see the documentation from EasyCaching on the matter
                    // mutex key's alive time(ms), default is 5000
                    config.LockMs = 6000;
                    // when mutex key alive, it will sleep some time, default is 300
                    config.SleepMs = 300;
                }, "nopCommerce_memory_cache");
                // distributed
                option.UseRedis(config =>
                {
                    config.Order = 1;
                    if (!string.IsNullOrEmpty(cacheSettings.Configuration))
                    {
                        config.DBConfig.Configuration = cacheSettings.Configuration;
                    }
                    else
                    {
                        config.DBConfig.Database = cacheSettings.DbId;
                        config.DBConfig.Endpoints.Add(cacheSettings.HostEndpoint);
                        if (!string.IsNullOrEmpty(cacheSettings.Password))
                        {
                            config.DBConfig.Password = cacheSettings.Password;
                        }
                        if (cacheSettings.Ssl)
                        {
                            config.DBConfig.IsSsl = cacheSettings.Ssl;
                        }
                        if (cacheSettings.ConnectTimeout > 0)
                        {
                            config.DBConfig.ConnectionTimeout = cacheSettings.ConnectTimeout;
                        }
                    }
                    config.EnableLogging = cacheSettings.EnableLogging; //to enable, see the documentation from EasyCaching on the matter
                    //To take advantage of this performance enhancement you have to modify the EasyCaching library to accept this parameter. The 0.5.6 version did not.
                    //Since the 0.5.6 was the last version to support .Net Core 2.2 you are stuck with this unless you fork your own copy of EasyCaching and use the code from later versions to add this feature.
                    //if (cacheSettings.PageSize > 0)
                    //{
                    //    config.DBConfig.PageSize = cacheSettings.PageSize;
                    //}
                }, "nopredis");

                // combine local and distributed
                option.UseHybrid(config =>
                {
                    config.TopicName = "nop-topic";
                        // specify the local cache provider name after v0.5.4
                        config.LocalCacheProviderName = "nopCommerce_memory_cache";
                        // specify the distributed cache provider name after v0.5.4
                        config.DistributedCacheProviderName = "nopredis";
                    config.EnableLogging = cacheSettings.EnableLogging;
                })
                // use redis bus
                .WithRedisBus(busConf =>
                {
                    if (!string.IsNullOrEmpty(cacheSettings.BusConfiguration))
                    {
                        busConf.Configuration = cacheSettings.BusConfiguration;
                    }
                    else
                    {
                        busConf.Database = cacheSettings.BusDbId;
                        busConf.Endpoints.Add(cacheSettings.BusEndpoint);
                        if (!string.IsNullOrEmpty(cacheSettings.Password))
                        {
                            busConf.Password = cacheSettings.Password;
                        }
                        if (cacheSettings.Ssl)
                        {
                            busConf.IsSsl = cacheSettings.Ssl;
                        }
                        if (cacheSettings.ConnectTimeout > 0)
                        {
                            busConf.ConnectionTimeout = cacheSettings.ConnectTimeout;
                        }
                    }
                });
            });

            // Registers a hosted service background worker, which periodically looks for clear cache events
            services.AddSingleton<IHostedService, ServerSyncService>();
        }

        private CacheBackplaneSettings ParseCacheSettings(IConfiguration configuration)
        {
            var dbid = 3;               //default
            var busdbid = 4;            //default
            var enableLogging = false;  //default
            var cachelimit = 200000;    //default
            var enablecompression = true;   //default
            var pageSize = 0;           //default

            var dbidStr = configuration["CacheBackplane:dbid"];
            var busdbidStr = configuration["CacheBackplane:busdbid"];
            var enableLoggingStr = configuration["CacheBackplane:enablelogging"];
            var config = configuration["CacheBackplane:configuration"];
            var busconfig = configuration["CacheBackplane:busconfiguration"];
            var cachelimitStr = configuration["CacheBackplane:cachelimit"];
            var pageSizeStr = configuration["CacheBackplane:pageSize"];

            int.TryParse(dbidStr, out dbid);
            int.TryParse(busdbidStr, out busdbid);
            bool.TryParse(enableLoggingStr, out enableLogging);
            int.TryParse(cachelimitStr, out cachelimit);
            int.TryParse(pageSizeStr, out pageSize);

            if (!string.IsNullOrEmpty(config) && !string.IsNullOrEmpty(busconfig))
            {
                return new CacheBackplaneSettings
                {
                    DbId = dbid,
                    BusDbId = busdbid,
                    EnableLogging = enableLogging,
                    Configuration = config,
                    BusConfiguration = busconfig,
                    CacheLimit = cachelimit,
                    PageSize = pageSize
                };
            }

            var connectTimeout = 5000;  //default
            var ssl = false;            //default

            var host = configuration["CacheBackplane:host"];
            var portStr = configuration["CacheBackplane:port"];
            var busPortStr = configuration["CacheBackplane:busPort"];
            var password = configuration["CacheBackplane:password"];
            var connectTimeoutStr = configuration["CacheBackplane:connectTimeout"];
            var sslStr = configuration["CacheBackplane:ssl"];

            int.TryParse(portStr, out int port);
            int.TryParse(busPortStr, out int busPort);
            int.TryParse(connectTimeoutStr, out connectTimeout);
            bool.TryParse(sslStr, out ssl);

            return new CacheBackplaneSettings
            {
                Host = host,
                Port = port,
                BusPort = busPort,
                ConnectTimeout = connectTimeout,
                DbId = dbid,
                BusDbId = busdbid,
                Password = password,
                Ssl = ssl,
                EnableLogging = enableLogging,
                HostEndpoint = new ServerEndPoint { Host = host, Port = port }, 
                BusEndpoint = new ServerEndPoint { Host = host, Port = busPort },
                Configuration = config,
                BusConfiguration = busconfig,
                CacheLimit = cachelimit,
                PageSize = pageSize
            };
        }
        private ThreadSettings ParseThreadSettings(IConfiguration configuration) 
        {
            var threadSettings = new ThreadSettings();
            var minIoThreadsStr = configuration["CacheBackplane:minIoThreads"];
            var minWorkerThreadsStr = configuration["CacheBackplane:minWorkerThreads"];
            if (int.TryParse(minIoThreadsStr, out int ithreads))
            {
                threadSettings.MinIoThreads = ithreads;
            }
            if (int.TryParse(minWorkerThreadsStr, out int wthreads))
            {
                threadSettings.MinWorkerThreads = wthreads;
            }
            return threadSettings;
        }
    }

    internal class ThreadSettings
    {
        public int MinWorkerThreads { get; internal set; }
        public int MinIoThreads { get; internal set; }
    }

    internal class CacheBackplaneSettings
    {
        public int DbId { get; set; }
        public int BusDbId { get; internal set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public int BusPort { get; set; }
        public string Password { get; set; }
        public int ConnectTimeout { get; set; }
        public bool Ssl { get; set; }
        public bool EnableLogging { get; set; }
        public ServerEndPoint HostEndpoint { get; set; }
        public ServerEndPoint BusEndpoint { get; set; }
        public string Configuration { get; set; }
        public string BusConfiguration { get; set; }
        public int CacheLimit { get; set; }
        public int PageSize { get; set; }
    }

    [Serializable]
    public class InvalidRedisBusConfigurationException : Exception
    {
        public InvalidRedisBusConfigurationException()
        {
        }

        public InvalidRedisBusConfigurationException(string message) : base(message)
        {
        }

        public InvalidRedisBusConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRedisBusConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
