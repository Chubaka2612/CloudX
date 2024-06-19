using System;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.SLESS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Dto;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.MON.Functional
{
    public class MONFunctionalValidationTest : ImageBaseTest
    {
        private const string MONTestDataFilePath = "MON\\mon_test_data.json";

        private readonly DateTime startTime = DateTime.UtcNow.AddMinutes(-1); //Start time (1 minutes ago)
        private readonly DateTime endTime = DateTime.UtcNow.AddMinutes(1); //End time (current time + 1 minute)
        private readonly int defaultWaitInMs = 60000;
        private readonly int defaultPollingInMs = 2000;

        [SetUp]
        protected void BeforeEach()
        {
            Log.Debug("Initialize Rest client");
            var publicIp = ConfigurationManager.GetConfiguration(MONTestDataFilePath)["PublicIP"];
            ImageApiEndpoint = ConfigurationManager.GetConfiguration(MONTestDataFilePath)["BaseImageApiEndpoint"];
            MyRestClient = new RestClient($"http://{publicIp}");
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-07")]
        public void GetImageApiRequestsIsLoggedInCloudWatchLogs()
        {
            var logGroup = "/var/log/cloudxserverless-app";
            var message = $"{Method.Get.ToString().ToUpper()} {ImageApiEndpoint}";

            //api action
            var getRequest = new RestRequest(ImageApiEndpoint, Method.Get);
            var getResponse = MyRestClient.Execute(getRequest);

            //Verify new log is present in logGroup
            AssertHelper.BecomesEqual(1,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result.Count,
                businessContext: $"Verify {message} is present in log group: {logGroup}",
               defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-07")]
        public async Task GetImageByIdApiRequestsIsLoggedInCloudWatchLogs()
        {
            var logGroup = "/var/log/cloudxserverless-app";

            var imageId = RandomStringUtils.RandomNumeric(5);
            var message = $"{Method.Get.ToString().ToUpper()} {ImageApiEndpoint}/{imageId}";

            //api action
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}", Method.Get);
            var getResponse = MyRestClient.Execute(getRequest);

            //Verify new log is present in logGroup
            AssertHelper.BecomesEqual(1,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result.Count,
                businessContext: $"Verify {message} is present in log group: {logGroup}",
               defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-07")]
        public async Task DeleteImageByIdApiRequestsIsLoggedInCloudWatchLogs()
        {
            var logGroup = "/var/log/cloudxserverless-app";

            var imageId = RandomStringUtils.RandomNumeric(5);
            var message = $"{Method.Delete.ToString().ToUpper()} {ImageApiEndpoint}/{imageId}";

            //api action
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}", Method.Delete);
            var getResponse = MyRestClient.Execute(getRequest);

            //Verify new log is present in logGroup
            AssertHelper.BecomesEqual(1,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result.Count,
                businessContext: $"Verify {message} is present in log group: {logGroup}",
               defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-07")]
        public async Task PostImageApiRequestsIsLoggedInCloudWatchLogs()
        {
            var logGroup = "/var/log/cloudxserverless-app";

            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: upload image
            UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);
            var message = $"{Method.Post.ToString().ToUpper()} {ImageApiEndpoint}";

            //Verify new log is present in logGroup
            AssertHelper.BecomesEqual(1,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result.Count,
                businessContext: $"Verify {message} is present in log group: {logGroup}",
               defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-06")]
        public async Task UploadEventProcessedByEventHandlerLambdaIsLoggedInCloudWatchLogs()
        {
            var logGroupPrefix = "/aws/lambda/cloudxserverless-EventHandlerLambda";

            //retrieve all LogGroups by prefix
            var logGroups = await CloudWatchLogsService.Instance().GetLogGroupsByPrefixAsync(logGroupPrefix);
            var requiredLogGroup = string.Empty;

            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: upload image
            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            var message = "SNS Client: Published message body: event_type: upload";

            //Verify new log is present in any logGroups
            AssertHelper.BecomesEqual(true,
            () =>
            {
                foreach (var logGroup in logGroups)
                {
                    var logs = CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result;
                    if (logs.Count > 0)
                    {
                        requiredLogGroup = logGroup; // remember logGroup name for further analysis
                        return true;
                    }
                }
                return false;
            },
            businessContext: $"Verify {message} is present in log group: {logGroupPrefix}",
            defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));

            VerifyImageMetaIsLoggedInCloudWatchLogs(requiredLogGroup, imageDto);
        }

        [Test]
        [Component(ComponentName.CloudX_MON)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-MON-06")]
        public async Task DeleteEventProcessedByEventHandlerLambdaIsLoggedInCloudWatchLogs()
        {
            var logGroupPrefix = "/aws/lambda/cloudxserverless-EventHandlerLambda";

            //retrieve all LogGroups by prefix
            var logGroups = await CloudWatchLogsService.Instance().GetLogGroupsByPrefixAsync(logGroupPrefix);
            var requiredLogGroup = string.Empty;

            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: upload image and delete
            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            var deleteRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);
            var message = "SNS Client: Published message body: event_type: delete";

            //Verify new log is present in any logGroups
            AssertHelper.BecomesEqual(true,
            () =>
            {
                foreach (var logGroup in logGroups)
                {
                    var logs = CloudWatchLogsService.Instance().FilterLogEventsAsync(logGroup, message, startTime, endTime).Result;
                    if (logs.Count > 0)
                    {
                        requiredLogGroup = logGroup; // remember logGroup name for further analysis
                        return true;
                    }
                }
                return false;
            },
            businessContext: $"Verify {message} is present in log group: {logGroupPrefix}",
            defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));

            VerifyImageMetaIsLoggedInCloudWatchLogs(requiredLogGroup, imageDto);
        }

        private void VerifyImageMetaIsLoggedInCloudWatchLogs(string requiredLogGroup, ImageDto imageDto) 
        {
            //For each notification, the image information (object key, object type, object size, etc)
            //is logged in the Event Handler Lambda logs in CloudWatch logs.
            var messageObjectKey = $"object_key: {imageDto.ObjectKey}";
            var messageObjectType = $"object_type: {imageDto.ObjectType}";
            var messageObjectSize = $"object_size: {imageDto.ObjectSize}";

            AssertHelper.BecomesEqual(true,
              () => CloudWatchLogsService.Instance().FilterLogEventsAsync(requiredLogGroup, messageObjectKey, startTime, endTime).Result.Count > 0,
              businessContext: $"Verify {messageObjectKey} is present in log group: {requiredLogGroup}",
              defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));

            AssertHelper.BecomesEqual(true,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(requiredLogGroup, messageObjectType, startTime, endTime).Result.Count > 0,
                businessContext: $"Verify {messageObjectType} is present in log group: {requiredLogGroup}",
                defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));

            AssertHelper.BecomesEqual(true,
                () => CloudWatchLogsService.Instance().FilterLogEventsAsync(requiredLogGroup, messageObjectSize, startTime, endTime).Result.Count > 0,
                businessContext: $"Verify {messageObjectSize} is present in log group: {requiredLogGroup}",
                defaultWait: TimeSpan.FromMilliseconds(defaultWaitInMs), defaultPolling: TimeSpan.FromMilliseconds(defaultPollingInMs));
        }
    }
}
