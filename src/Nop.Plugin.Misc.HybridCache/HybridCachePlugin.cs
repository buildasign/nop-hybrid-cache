using BAS.Nop.Plugin.Misc.HybridCache;
using BAS.Nop.Plugin.Misc.HybridCache.Common;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Plugins;

namespace BAS.Nop.Plugin.Misc.Services
{
    public class HybridCachePlugin : BasePlugin, IMiscPlugin
    {
        private readonly IWebHelper _webHelper;
        private readonly ISettingService _settingService;

        public HybridCachePlugin(IWebHelper webHelper, ISettingService settingService) 
        {
            _webHelper = webHelper;
            _settingService = settingService;
        }

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + RouteConstants.ConfigurationPathAbsolute;
        }

        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new HybridCacheSettings());

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<HybridCacheSettings>();

            base.Uninstall();
        }
    }
}
