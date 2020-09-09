using Nop.Core.Domain.Customers;
using Nop.Services.Plugins;
using System.Collections.Generic;

namespace Nop.Plugin.Sms.Clickatell.Controllers
{
    public  interface ISMSPluginFinder : IPluginManager<ISMSMethod>
    {
        /// <summary>
        /// Load active payment methods
        /// </summary>
        /// <param name="customer">Filter by customer; pass null to load all plugins</param>
        /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
        /// <param name="countryId">Filter by country; pass 0 to load all plugins</param>
        /// <returns>List of active payment methods</returns>
        IList<ISMSMethod> LoadActivePlugins(Customer customer = null, int storeId = 0, int countryId = 0);

        /// <summary>
        /// Check whether the passed payment method is active
        /// </summary>
        /// <param name="paymentMethod">Payment method to check</param>
        /// <returns>Result</returns>
        bool IsPluginActive(ISMSMethod smsMethod);

        /// <summary>
        /// Check whether the payment method with the passed system name is active
        /// </summary>
        /// <param name="systemName">System name of payment method to check</param>
        /// <param name="customer">Filter by customer; pass null to load all plugins</param>
        /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
        /// <returns>Result</returns>
        bool IsPluginActive(string systemName, Customer customer = null, int storeId = 0);

        /// <summary>
        /// Get countries in which the passed payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>List of country identifiers</returns>
        IList<int> GetRestrictedCountryIds(ISMSMethod smsMethod);

        /// <summary>
        /// Save countries in which the passed payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="countryIds">List of country identifiers</param>
      
    }
}