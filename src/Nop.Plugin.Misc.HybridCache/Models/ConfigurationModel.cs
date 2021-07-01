using System.Collections.Generic;
using System.ComponentModel;

namespace BAS.Nop.Plugin.Misc.HybridCache.Models
{
    public class ConfigurationModel
    {
        [DisplayName("Display Cache Stats")]
        public bool ShowCacheStats { get; set; }
        public Dictionary<string, string> CacheStats { get; set; }
        [DisplayName("Key To Search")]
        public string KeyToSearch { get; set; }
    }
}
