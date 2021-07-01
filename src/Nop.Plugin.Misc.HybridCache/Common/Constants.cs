namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public static class RouteConstants
    {
        public const string PluginPath = "~/Plugins/Misc.HybridCache";
        public const string ConfigurePluginPath = PluginPath + "/Views/Admin/Configure.cshtml";
        public const string ConfigurationPathAbsolute = "Admin/HybridCacheConfiguration/Configure";
    }
    public static class BusConstants 
    {
        public const string SYNC_TOPIC = "bas.nop.sync";
    }
    public static class BusCommands 
    {
        public const string SHOW_CACHE_STATS = "ShowCacheStats";
    }
}
