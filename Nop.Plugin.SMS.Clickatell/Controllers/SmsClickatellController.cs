using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Plugins;
using Nop.Plugin.Sms.Clickatell.Models;
using Nop.Plugin.SMS.Clickatell;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Sms.Clickatell.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class SmsClickatellController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public SmsClickatellController(ILocalizationService localizationService,
            IPluginFinder pluginFinder,
            ISettingService settingService,
            IStoreService storeService,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            this._localizationService = localizationService;
            this._pluginFinder = pluginFinder;
            this._settingService = settingService;            
            this._storeService = storeService;
            this._workContext = workContext;
            this._permissionService = permissionService;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var clickatellSettings = _settingService.LoadSetting<ClickatellSettings>(storeScope);

            var model = new SmsClickatellModel
            {
                Enabled = clickatellSettings.Enabled,
                ApiId = clickatellSettings.ApiId,
                Password = clickatellSettings.Password,
                Username = clickatellSettings.Username,
                PhoneNumber = clickatellSettings.PhoneNumber,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.Enabled_OverrideForStore = _settingService.SettingExists(clickatellSettings, x => x.Enabled, storeScope);
                model.PhoneNumber_OverrideForStore = _settingService.SettingExists(clickatellSettings, x => x.PhoneNumber, storeScope);
            }

            return View("~/Plugins/SMS.Clickatell/Views/Configure.cshtml", model);
        }


        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public IActionResult Configure(SmsClickatellModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var clickatellSettings = _settingService.LoadSetting<ClickatellSettings>(storeScope);

            //save settings
            clickatellSettings.Enabled = model.Enabled;
            clickatellSettings.ApiId = model.ApiId;
            clickatellSettings.Username = model.Username;
            clickatellSettings.Password = model.Password;
            clickatellSettings.PhoneNumber = model.PhoneNumber;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSetting(clickatellSettings, x => x.ApiId, storeScope, false);
            _settingService.SaveSetting(clickatellSettings, x => x.Username, storeScope, false);
            _settingService.SaveSetting(clickatellSettings, x => x.Password, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(clickatellSettings, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(clickatellSettings, x => x.PhoneNumber, model.PhoneNumber_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("test")]
        public IActionResult TestSms(SmsClickatellModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Mobile.SMS.Clickatell");
            if (pluginDescriptor == null)
                throw new Exception("Cannot load the plugin");

            var plugin = pluginDescriptor.Instance() as ClickatellSmsProvider;
            if (plugin == null)
                throw new Exception("Cannot load the plugin");

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var clickatellSettings = _settingService.LoadSetting<ClickatellSettings>(storeScope);

            //test SMS send
            if (plugin.SendSms(model.TestMessage, 0, clickatellSettings))
                SuccessNotification(_localizationService.GetResource("Plugins.Sms.Clickatell.TestSuccess"));
            else
                ErrorNotification(_localizationService.GetResource("Plugins.Sms.Clickatell.TestFailed"));

            return Configure();
        }

        #endregion
    }
}