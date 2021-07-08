namespace EasyCaching.Redis
{
    using EasyCaching.Core.Configurations;

    /// <summary>
    /// Redis cache options.
    /// </summary>
    public class RedisDBOptions : BaseRedisOptions
    {        
        /// <summary>
        /// Gets or sets the Redis database index the cache will use.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
       public int Database { get; set; } = 0;
       /// <summary>
       /// Gets or sets the SCAN page size (COUNT).
       /// </summary>
       public int PageSize { get; set; } = 200;
    }
}
