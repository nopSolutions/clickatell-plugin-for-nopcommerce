using Nop.Services.Plugins;

namespace Nop.Plugin.Sms.Clickatell.Controllers
{
    public interface ISMSMethod : IPlugin
    {
        string GetPublicViewComponentName();
    }
}