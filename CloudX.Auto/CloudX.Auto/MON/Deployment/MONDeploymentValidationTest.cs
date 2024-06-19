using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon;
using CloudX.Auto.AWS.Core.Domain.MON;
using CloudX.Auto.AWS.Core.Domain.SLESS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using NUnit.Framework;

namespace CloudX.Auto.Tests.MON.Deployment
{
    public class MONDeploymentValidationTest : ImageBaseTest
    {
        private const string MONTestDataFilePath = "MON\\mon_test_data.json";
        private JsonObject properties;

        [SetUp]
        protected void BeforeEach()
        {
            properties = ReadConfig(MONTestDataFilePath);
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-01")]
        public async Task ApplicationEC2InstanceHasCloudWatchIntegration()
        {
            var instanceId = properties["InstaneId"].ToString();

            var metrics =
                await CloudWatchService.Instance.GetCloudWatchMetricsAsync(instanceId);

            //EC2 instance has CloudWatch integration if the CloudWatch alarms, metrics, or logs associated with the instance
            AssertHelper.IsTrue(metrics.Count > 0, "Verify application EC2 instance has CloudWatch integration");
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-02")]
        public void CloudInitLogsShouldBeCollectedInCloudWatcLogs()
        {
            var instanceId = properties["InstaneId"].ToString();
            var region = RegionEndpoint.USEast1;
            var logGroupName = "/var/log/cloud-init";
           
            AssertHelper.AssertScope(
            () => AssertHelper.IsNotNull(CloudWatchLogsService.Instance(region).GetCloudWatchLogGroupAsync(
                      logGroupName), "Verify the log group exists"),
            () => AssertHelper.IsTrue(CloudWatchLogsService.Instance(region).GetCloudWatchLogStreamsAsync(logGroupName, instanceId).Result.Count > 0,
                "Verify there are log streams for the given log group")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-03")]
        public void ApplicationMessagesShouldBeCollectedInCloudWatchLogs()
        {
            var logGroupName = "/var/log/cloudxserverless-app";
    
            AssertHelper.AssertScope(
            () => AssertHelper.IsNotNull(CloudWatchLogsService.Instance().GetCloudWatchLogGroupAsync(
                      logGroupName), "Verify the log group exists"),
            () => AssertHelper.IsTrue(CloudWatchLogsService.Instance().GetCloudWatchLogStreamsAsync(logGroupName).Result.Count > 0,
                "Verify there are log streams for the given log group")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-04")]
        public async Task EventHandlerLogsShouldBeCollectedInCloudWatchLogs()
        {
            var logGroupName = "/aws/lambda/cloudxserverless-EventHandlerLambda";

            var group = (await CloudWatchLogsService.Instance().GetCloudWatchLogGroupAsync(logGroupName));
            AssertHelper.AssertScope(
            () => AssertHelper.IsNotNull(group, "Verify the log group exists"),
            () => AssertHelper.IsTrue(CloudWatchLogsService.Instance().GetCloudWatchLogStreamsAsync(group.LogGroupName).Result.Count > 0,
                "Verify there are log streams for the given log group")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-05")]
        public async Task CloudTrailHasCorrectConfiguration()
        {
            #region Properties

            var traiConfig = properties["Trail"];
            var name = traiConfig["Name"].ToString();
            var isMultRegion = bool.Parse(traiConfig["MultRegion"].ToString());
            var isLogFileValidation = bool.Parse(traiConfig["LogFileValidation"].ToString());
            var isSSEKMSEncryption = bool.Parse(traiConfig["SSEKMSEncryption"].ToString());
            var expectedTag = traiConfig["Tags"].AsArray().Select(t => t.ToString()).ToArray().First();

            #endregion

            var trail = await CloudTrailService.Instance.GetCloudTrailDescription(name);
            bool hasRequiredTag = false;
            var actualTags = CloudTrailService.Instance.GetCloudTrailTagsAsync(trail.TrailARN).Result;
            foreach (var resourceTag in actualTags)
            {
                foreach (var tag in resourceTag.TagsList)
                {
                    if (tag.Key == expectedTag.Split(":").First() && tag.Value == expectedTag.Split(":").Last())
                    {
                        hasRequiredTag = true;
                        break;
                    }
                }
            }
            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(trail.IsMultiRegionTrail, isMultRegion,
                "Verify Trail has correct value of 'Multi-region': yes"),
                () => AssertHelper.AreEquals(trail.LogFileValidationEnabled, isLogFileValidation,
                "Verify Trail has correct value of 'Log file validation': enabled"),
                () => AssertHelper.AreEquals(!string.IsNullOrEmpty(trail.KmsKeyId), isSSEKMSEncryption,
                "Verify Trail has correct value of 'SSE-KMS encryption': not enabled"),
                () => AssertHelper.IsTrue(hasRequiredTag, "Verify required tag is present")
                );
        }
    }
}
