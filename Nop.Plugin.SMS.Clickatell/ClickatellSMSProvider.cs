using System;
using System.Linq;
using System.ServiceModel;
using System.Web.Routing;
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

        #endregion

        #region Ctor

        public ClickatellSmsProvider(ClickatellSettings clickatellSettings,
            ILogger logger,
            IOrderService orderService,
            ISettingService settingService)
        {
            this._clickatellSettings = clickatellSettings;
            this._logger = logger;
            this._orderService = orderService;
            this._settingService = settingService;
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
                text = string.Format("New order #{0} was placed for the total amount {1:0.00}", order.Id, order.OrderTotal);

            using (var smsClient = new ClickatellSmsClient(new BasicHttpBinding(), new EndpointAddress("http://api.clickatell.com/soap/document_literal/webservice")))
            {
                //check credentials
                var authentication = smsClient.auth(int.Parse(clickatellSettings.ApiId), clickatellSettings.Username, clickatellSettings.Password);
                if (!authentication.ToUpperInvariant().StartsWith("OK"))
                {
                    _logger.Error(string.Format("Clickatell SMS error: {0}", authentication));
                    return false;
                }

                //send SMS
                var sessionId = authentication.Substring(4);
                var result = smsClient.sendmsg(sessionId, int.Parse(clickatellSettings.ApiId), clickatellSettings.Username, clickatellSettings.Password,
                    text, new [] { clickatellSettings.PhoneNumber }, string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    string.Empty, 0, string.Empty, string.Empty, string.Empty, 0).FirstOrDefault();

                if (result == null || !result.ToUpperInvariant().StartsWith("ID"))
                {
                    _logger.Error(string.Format("Clickatell SMS error: {0}", result));
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

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "SmsClickatell";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.SMS.Clickatell.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new ClickatellSettings());

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId", "API ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId.Hint", "Specify Clickatell API ID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled", "Enabled");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled.Hint", "Check to enable SMS provider.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password", "Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password.Hint", "Specify Clickatell API password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber", "Phone number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber.Hint", "Enter your phone number.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage", "Message text");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage.Hint", "Enter text of the test message.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username", "Username");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username.Hint", "Specify Clickatell API username.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.SendTest", "Send");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.SendTest.Hint", "Send test message");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.TestFailed", "Test message sending failed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Clickatell.TestSuccess", "Test message was sent");

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
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.ApiId.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Enabled.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.PhoneNumber.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.TestMessage.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.Fields.Username.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.SendTest");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.SendTest.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.TestFailed");
            this.DeletePluginLocaleResource("Plugins.Sms.Clickatell.TestSuccess");

            base.Uninstall();
        }

        #endregion
    }
}
