namespace EasyCaching.Serialization.Json
{
    using EasyCaching.Core.Configurations;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// EasyCaching options extensions.
    /// </summary>
    public static class EasyCachingOptionsExtensions
    {
        /// <summary>
        /// Withs the json.
        /// </summary>
        /// <returns>The json.</returns>
        /// <param name="options">Options.</param>        
        public static EasyCachingOptions WithJson(this EasyCachingOptions options) => options.WithJson(configure: x => { });
        /// <summary>
        /// Withs the json.
        /// </summary>
        /// <returns>The json.</returns>
        /// <param name="options">Options.</param>
        /// <param name="configure">Configure.</param>
        public static EasyCachingOptions WithJson(this EasyCachingOptions options, Action<EasyCachingJsonSerializerOptions> configure)
        {
            options.RegisterExtension(new JsonOptionsExtension(configure));

            return options;
        }

        /// <summary>
        /// Withs the json serializer.
        /// </summary>        
        /// <param name="options">Options.</param>
        /// <param name="jsonSerializerSettingsConfigure">Configure serializer settings.</param>
        /// <param name="name">The name of this serializer instance.</param>     
        public static EasyCachingOptions WithJson(this EasyCachingOptions options, Action<JsonSerializerSettings> jsonSerializerSettingsConfigure)
        {
            options.RegisterExtension(new JsonOptionsExtension(jsonSerializerSettingsConfigure));

            return options;
        }

        /// <summary>
        /// Withs the json compressed serializer.
        /// </summary>        
        /// <param name="options">Options.</param>
        /// <param name="jsonSerializerSettingsConfigure">Configure serializer settings.</param>
        /// <param name="name">The name of this serializer instance.</param>     
        public static EasyCachingOptions WithJsonCompressed(this EasyCachingOptions options, Action<JsonSerializerSettings> jsonSerializerSettingsConfigure)
        {
            options.RegisterExtension(new JsonOptionsExtension(jsonSerializerSettingsConfigure, true));

            return options;
        }

    }
}
