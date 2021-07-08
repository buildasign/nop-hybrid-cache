namespace EasyCaching.Serialization.Json
{
    using System;
    using EasyCaching.Core.Configurations;
    using EasyCaching.Core.Serialization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;

    /// <summary>
    /// Json options extension.
    /// </summary>
    internal sealed class JsonOptionsExtension : IEasyCachingOptionsExtension
    {
        /// <summary>
        /// The configure.
        /// </summary>
        private readonly Action<EasyCachingJsonSerializerOptions> _configure;
        private Action<JsonSerializerSettings> _jsonSerializerSettingsConfigure;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EasyCaching.Serialization.Json.JsonOptionsExtension"/> class.
        /// </summary>
        /// <param name="configure">Configure.</param>
        public JsonOptionsExtension(Action<EasyCachingJsonSerializerOptions> configure)
        {
            this._configure = configure;
        }

        public JsonOptionsExtension(Action<JsonSerializerSettings> jsonSerializerSettingsConfigure)
        {
            this._jsonSerializerSettingsConfigure = jsonSerializerSettingsConfigure;
        }

        /// <summary>
        /// Adds the services.
        /// </summary>
        /// <param name="services">Services.</param>
        public void AddServices(IServiceCollection services)
        {
            services.AddOptions();
            if (_jsonSerializerSettingsConfigure != null)
            {
                var name = "json";
                services.Configure(name, _jsonSerializerSettingsConfigure);
                services.AddSingleton<IEasyCachingSerializer, DefaultJsonSerializer>(x =>
                {
                    var optionsMon = x.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JsonSerializerSettings>>();
                    var options = optionsMon.Get(name);
                    return new DefaultJsonSerializer(options);
                });
            }
            else
            {
                Action<EasyCachingJsonSerializerOptions> configure = x => { };
                if (_configure != null) configure = _configure;

                services.Configure(configure);
                services.AddSingleton<IEasyCachingSerializer, DefaultJsonSerializer>();
            }
        }

        /// <summary>
        /// Withs the services.
        /// </summary>
        /// <param name="services">Services.</param>
        public void WithServices(IApplicationBuilder services)
        {
            // Method intentionally left empty.
        }
    }
}
