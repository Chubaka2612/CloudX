using System;
using System.IO;
using System.Reflection;
using System.Xml;
using CloudX.Auto.Core.Configuration.Models;
using CloudX.Auto.Core.Exceptions;
using Microsoft.Extensions.Configuration;

namespace CloudX.Auto.Core.Configuration
{
    public static class ConfigurationManager
    {
        private static XmlElement _logConfiguration;

        private static IConfiguration _configuration;
        
        private static IAMConfiguration _iamConfiguration;

        public static IConfiguration Configuration => GetConfiguration("appsettings.json");
       

        public static IAMConfiguration IAMConfiguration
        {
            get
            {
                _iamConfiguration ??= Get<IAMConfiguration>(nameof(IAMConfiguration));
                return _iamConfiguration;
            }
        }

        public static IConfiguration GetConfiguration(string fileName)
        {
         
            try
            {
                var directory = AppDomain.CurrentDomain.BaseDirectory;
                _configuration = new ConfigurationBuilder().SetBasePath(directory)
                    .AddJsonFile(fileName)
                    .AddUserSecrets(Assembly.GetCallingAssembly())
                    .AddEnvironmentVariables()
                    .Build();

                return _configuration;
            }
            catch (Exception ex)
            {
                throw new InitializationException($"Can't load config file {fileName}:" + ex.Message);
            }
        }

        public static XmlElement GetLogConfiguration
        {
            get
            {
                if (_logConfiguration == null)
                {
                    var log4NetConfig = new XmlDocument();
                    try
                    {
                        log4NetConfig.Load(File.OpenRead("log4net.config"));
                        _logConfiguration = log4NetConfig["log4net"];
                    }
                    catch (Exception ex)
                    {
                        throw new InitializationException($"Can't load config file for log: " + ex.Message);
                    }
                }
                return _logConfiguration;
            }
        }

        private static T Get<T>(string sectionName)
        {
            var section = Configuration.GetSection(sectionName);
            return section.Get<T>();
        }

        public static T Get<T>(string sectionName, string fileName)
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(directory);
            configBuilder.AddJsonFile(fileName, true);
            return configBuilder.Build().GetSection(sectionName).Get<T>();
        }
    }
}