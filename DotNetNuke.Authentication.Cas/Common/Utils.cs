using System.Diagnostics;
using System.Linq;

namespace DotNetNuke.Authentication.Cas.Common
{
    public class Utils
    {
        [DebuggerStepThrough]
        public static string GetAppSetting(string key, string defaultValue = "")
        {
            return System.Configuration.ConfigurationManager.AppSettings.AllKeys.Contains(key) ? System.Configuration.ConfigurationManager.AppSettings[key] : defaultValue;
        }
    }

}
