using System.IO;
using System.Text.Json.Nodes;
using CloudX.Auto.Core.Logging;
using log4net;
using NUnit.Framework;

namespace CloudX.Auto.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void BeforeAll()
        {
            LogConfigurator.Configure();
        }

    }

    [TestFixture]
    public class BaseTest
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(BaseTest));

        protected static JsonObject ReadConfig(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonNode.Parse(json).AsObject();
        }
    }
}
