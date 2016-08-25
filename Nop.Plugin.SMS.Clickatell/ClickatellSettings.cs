using Nop.Core.Configuration;

namespace Nop.Plugin.SMS.Clickatell
{
    public class ClickatellSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the value indicting whether this SMS provider is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the Clickatell API ID
        /// </summary>
        public string ApiId { get; set; }

        /// <summary>
        /// Gets or sets the Clickatell API username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the Clickatell API password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the store owner phone number
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}