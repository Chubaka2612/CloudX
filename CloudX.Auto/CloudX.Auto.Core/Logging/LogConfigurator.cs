using log4net;
using log4net.Config;
using System.Reflection;
using CloudX.Auto.Core.Configuration;

namespace CloudX.Auto.Core.Logging
{
    public static class LogConfigurator 
    {
        public static void Configure() 
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, ConfigurationManager.GetLogConfiguration);
        }
    }
}
