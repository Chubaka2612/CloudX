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
    }
}
