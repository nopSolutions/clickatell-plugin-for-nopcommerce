using System;
using System.Linq;
using System.ServiceModel;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Plugin.SMS.Clickatell.Clickatell;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.SMS.Clickatell
{
    /// <summary>
    /// Represents the Clickatell SMS provider
    /// </summary>
    public class ClickatellSmsProvider : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ClickatellSettings _clickatellSettings;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public ClickatellSmsProvider(ClickatellSettings clickatellSettings,
            ILogger logger,
            IOrderService orderService,
            ISettingService settingService,
            IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            this._clickatellSettings = clickatellSettings;
            this._logger = logger;
            this._orderService = orderService;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Send SMS 
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="orderId">Order id</param>
        /// <param name="settings">Clickatell settings</param>
        /// <returns>True if SMS was successfully sent; otherwise false</returns>
        public bool SendSms(string text, int orderId, ClickatellSettings settings = null)
        {
            var clickatellSettings = settings ?? _clickatellSettings;
            if (!clickatellSettings.Enabled)
                return false;

            //change text
            var order = _orderService.GetOrderById(orderId);
            if (order != null)
                text = $"New order #{order.Id} was placed for the total amount {order.OrderTotal:0.00}";

            using (var smsClient = new ClickatellSmsClient(new BasicHttpBinding(), new EndpointAddress("http://api.clickatell.com/soap/document_literal/webservice")))
            {
                //check credentials
                var authentication = smsClient.auth(int.Parse(clickatellSettings.ApiId), clickatellSettings.Username, clickatellSettings.Password);
                if (!authentication.ToUpperInvariant().StartsWith("OK"))
                {
                    _logger.Error($"Clickatell SMS error: {authentication}");
                    return false;
                }

                //send SMS
                var sessionId = authentication.Substring(4);
                var result = smsClient.sendmsg(sessionId, int.Parse(clickatellSettings.ApiId), clickatellSettings.Username, clickatellSettings.Password,
                    text, new [] { clickatellSettings.PhoneNumber }, string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    string.Empty, 0, string.Empty, string.Empty, string.Empty, 0).FirstOrDefault();

                if (result == null || !result.ToUpperInvariant().StartsWith("ID"))
                {
                    _logger.Error($"Clickatell SMS error: {result}");
                    return false;
                }
            }

            //order note
            if (order != null)
            {
                order.OrderNotes.Add(new OrderNote()
                {
                    Note = "\"Order placed\" SMS alert (to store owner) has been sent",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);
            }

            return true;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/SmsClickatell/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new ClickatellSettings());

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId", "API ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId.Hint", "Specify Clickatell API ID.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled", "Enabled");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled.Hint", "Check to enable SMS provider.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password", "Password");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password.Hint", "Specify Clickatell API password.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber", "Phone number");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber.Hint", "Enter your phone number.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage", "Message text");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage.Hint", "Enter text of the test message.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username", "Username");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username.Hint", "Specify Clickatell API username.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.SendTest", "Send");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.SendTest.Hint", "Send test message");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.TestFailed", "Test message sending failed");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.TestSuccess", "Test message was sent");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<ClickatellSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.SendTest");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.SendTest.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.TestFailed");
            _localizationService.DeletePluginLocaleResource("Plugins.Sms.Clickatell.TestSuccess");

            base.Uninstall();
        }

        #endregion
    }
}
