using System.Collections.Generic;
using BAS.Nop.Plugin.Misc.HybridCache.Common;
using BAS.Nop.Plugin.Misc.HybridCache.Models;
using BAS.Nop.Plugin.Misc.HybridCache.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Filters;

namespace BAS.Nop.Plugin.Misc.HybridCache.Controllers
{
    public class HybridCacheConfigurationController : BaseAdminController
    {
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly IBusAccessor _busAccessor;
        private readonly IDistributedCacheStatMonitor _distributedCacheStatMonitor;

        public HybridCacheConfigurationController(IPermissionService permissionService,
            ISettingService settingService,
            INotificationService notificationService, IBusAccessor busAccessor, IDistributedCacheStatMonitor distributedCacheStatMonitor) 
        {
            _permissionService = permissionService;
            _settingService = settingService;
            _notificationService = notificationService;
            _busAccessor = busAccessor;
            _distributedCacheStatMonitor = distributedCacheStatMonitor;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
            };

            return View(RouteConstants.ConfigurePluginPath, model);
        }

        /// <summary>
        /// Saves settings for plugin captured from view
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AdminAntiForgery]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
                return AccessDeniedView();

            if (model.ShowCacheStats)
            {
                var message = new ServerSyncCommandData { Command = BusCommands.SHOW_CACHE_STATS, Data = new[] { model.KeyToSearch } };
                var messagejson = JsonConvert.SerializeObject(message);
                _busAccessor.Publish(BusConstants.SYNC_TOPIC, messagejson);
                model.CacheStats = _distributedCacheStatMonitor.GetAllCacheStats(model.KeyToSearch);
                model.CacheStats.Add("Local Results:", "Request made. Check logs.");
            }

            return View(RouteConstants.ConfigurePluginPath, model);
        }
    }
}
