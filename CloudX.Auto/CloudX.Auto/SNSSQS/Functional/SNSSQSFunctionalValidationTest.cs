using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CloudX.Auto.AWS.Core.Domain.SNS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Dto;
using CloudX.Auto.Tests.Models.TestData;
using CloudX.Auto.Tests.Steps.SNSSQS;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.SNSSQS.Functional
{
    [NonParallelizable]
    public class SNSSQSFunctionalValidationTest : ImageBaseTest
    {
        private const string SnsSqsTestDataFilePath = "SNSSQS\\snssqs_test_data.json";
        
        protected string PublicIp = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["PublicIP"];
        protected string NotificationApiEndpoint = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["BaseNotificationApiEndpoint"];
        protected string InboxId = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["InboxId"];
        protected string UserEmail = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["Email"];
        protected static SnsTopic SourceSnsTopic = ConfigurationManager.Get<SnsTopic>(nameof(SnsTopic),
            SnsSqsTestDataFilePath);

        protected MailslurpClient MailslurpClient;
        
        [SetUp]
        protected void BeforeEach()
        {
            
            Log.Debug("Initialize Rest client");
            ImageApiEndpoint = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["BaseImageApiEndpoint"];
            MyRestClient = new RestClient($"http://{PublicIp}");

            Log.Debug("Initialize Mailslurp client");
            var apiKey = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["MailslurpApiKey"];
            MailslurpClient = new MailslurpClient(apiKey);
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-04")]
        public void SNSSQSUserCanSubscribeToNotificationsViaProvidedEmail()
        {
            //api action
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode, 
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-05")]
        public async Task SNSSQSUserHasToConfirmSubscriptionAfterReceivingConfirmationEmail()
        {
            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");
            AssertHelper.IsTrue(email.Body.Contains("You have chosen to subscribe to the topic"),
                $"Verify user with email {UserEmail} has got subscription email");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: verify user was subscribed via GET: /notification
            var getRequest = new RestRequest(NotificationApiEndpoint);
            var getResponse = MyRestClient.Execute(getRequest);

            var notificationDto = JsonConvert.DeserializeObject<List<NotificationDto>>(getResponse.Content)
                .FirstOrDefault(notificationDto => notificationDto.Endpoint == UserEmail);
            AssertHelper.IsNotNull(notificationDto, $"Verify notifications with Endpoint: {UserEmail} exists");

            var snsTopic = (await SNSService.Instance.ListTopicsAsync()).Topics.FirstOrDefault(topic =>
                               topic.TopicArn.Contains(SourceSnsTopic.Name))
                           ?? throw new Exception($"No SNS Topics with name '{SourceSnsTopic.Name}'");
            AssertHelper.AssertScope(
                () => AssertHelper.IsFalse(notificationDto.SubscriptionArn.Contains("PendingConfirmation"),
                    "Verify Notification.SubscriptionArn is correct"),
                () => AssertHelper.AreEquals(notificationDto.TopicArn, snsTopic.TopicArn,
                    "Verify Notification.TopicArn is correct")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-06")]
        public void SNSSQSSubscribedUserReceivesNotificationsAboutImagesEvents_UploadImage()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: upload image and get imageDto for further verification
            var imageId = UploadFileViaApi(filePath, imageName, fileNameToUpload);
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            //the notification contains the correct image metadata information
            var emailWithEvent = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification Message");

            VerifyEmailContentCorrespondsToExpectedMetadata(imageDto, "upload", emailWithEvent.Body);
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-06")]
        public void SNSSQSSubscribedUserReceivesNotificationsAboutImagesEvents_DeleteImage()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: upload image and get imageDto for further deletion
            var imageId = UploadFileViaApi(filePath, imageName, fileNameToUpload);
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            //api action: delete uploaded image
            var deleteRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify DELETE {deleteResponse.ResponseUri} has 'Success' status code");
           
            //the notification contains the correct image metadata information
            var emailWithEvent = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification Message");

            VerifyEmailContentCorrespondsToExpectedMetadata(imageDto, "delete", emailWithEvent.Body);
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-08")]
        public void SNSSQSUserCanDownloadImageUsingDownloadLinkFromNotification()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var fileNameDownloaded = imageName.Split('.').First() + "_downloaded_" + RandomStringUtils.RandomString(6) + ".jpg";
            var sourcesFilePath = "SNSSQS/Resources";
            var downloadedFilePath = Path.Combine(sourcesFilePath, "Downloads");

            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post); 
            MyRestClient.Execute(postRequest);

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: upload image for further downloading from notification
            UploadFileViaApi(sourcesFilePath, imageName, fileNameToUpload);

            //the notification contains link for downloading image
            var emailWithEvent = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification Message");
            //parse downloaded link
            var downloadLinkPattern = @"download_link:\s*(.*)";
            var downloadLink = ExtractValue(emailWithEvent.Body, downloadLinkPattern);

            //api action: download image by link
            var getRequest = new RestRequest(downloadLink);
            var getResponse = MyRestClient.Execute(getRequest);
            AssertHelper.IsTrue(getResponse.IsSuccessStatusCode,
                $"Verify GET {getResponse.ResponseUri} has 'Success' status code");
            File.Create(Path.Combine(downloadedFilePath, fileNameDownloaded)).Close();
            File.WriteAllBytes(Path.Combine(downloadedFilePath, fileNameDownloaded), getResponse.RawBytes);

            //verify file was downloaded
            AssertHelper.IsTrue(FilesEqual(Path.Combine(sourcesFilePath, imageName), Path.Combine(downloadedFilePath, fileNameDownloaded)),
                "Verify downloaded file is as expected");
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-09")]
        public void SNSSQSUserCanUnsubscribeFromNotificationsAndDoesNotReceiveFurtherNotifications()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SNSSQS\\Resources";

            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: unsubscribe user
            var deleteRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify DELETE {deleteResponse.ResponseUri} has 'Success' status code");

            //api action: verify user was subscribed via GET: /notification
            var getRequest = new RestRequest(NotificationApiEndpoint);
            var getResponse = MyRestClient.Execute(getRequest);
            var notificationDto = JsonConvert.DeserializeObject<List<NotificationDto>>(getResponse.Content)
                .FirstOrDefault(notificationDto => notificationDto.Endpoint == UserEmail);
            AssertHelper.IsNull(notificationDto, $"Verify notifications with Endpoint: {UserEmail} doesn't exist");

            //api action: trigger event (upload image) to check no further notification sending is occured for unsubscribed user
            MailslurpClient.DeleteAllInboxEmails(Guid.Parse(InboxId));//delete ll present emails in inbox
            
            UploadFileViaApi(filePath, imageName, fileNameToUpload); 
            
            //email action: check that the user did not receive a notification
            var actualEmailsCount = MailslurpClient.GetInboxEmailCount(Guid.Parse(InboxId));
            AssertHelper.AreEquals(actualEmailsCount, 0, "Verify that user did not receive new notification");
        }

        [Test]
        [Component(ComponentName.CloudX_SNS_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-11")]
        public void SNSSQSItIsPossibleToViewAllExistingSubscriptions()
        {
            //api action: subscribe user
            var postRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Post);
            var postResponse = MyRestClient.Execute(postRequest);
            AssertHelper.IsTrue(postResponse.IsSuccessStatusCode,
                $"Verify POST {postResponse.ResponseUri} has 'Success' status code");

            //email verification action: get URL and confirm the subscription
            var email = MailslurpClient.GetLatestEmailByInbox(Guid.Parse(InboxId),
                "AWS Notification - Subscription Confirmation");

            //api action: confirm subscription via url
            ConfirmSubscriptionViaConfirmationUrlSentByEmail(email.Body);

            //api action: verify user was subscribed via GET: /notification
            var getRequest = new RestRequest(NotificationApiEndpoint);
            var getResponse = MyRestClient.Execute(getRequest);

            var notificationDtos = JsonConvert.DeserializeObject<List<NotificationDto>>(getResponse.Content);
            AssertHelper.AreEquals(notificationDtos.Count, 1, "Verify only 1 subscription exists");

            var notificationDto = notificationDtos.FirstOrDefault(notificationDto => notificationDto.Endpoint == UserEmail);
            AssertHelper.IsNotNull(notificationDto, $"Verify notifications with Endpoint: {UserEmail} exists");
        }

        private void VerifyEmailContentCorrespondsToExpectedMetadata(ImageDto expectedImageDto, string expectedEventType, string emailBody)
        {
            // Define regex patterns for each property
            var eventTypePattern = @"event_type:\s*(.*)";
            var objectKeyPattern = @"object_key:\s*(.*)";
            var objectTypePattern = @"object_type:\s*(.*)";
            var lastModifiedPattern = @"last_modified:\s*(.*)";
            var objectSizePattern = @"object_size:\s*(.*)";
         
            // Extract the properties using the regex patterns
            var eventType = ExtractValue(emailBody, eventTypePattern);
            var objectKey = ExtractValue(emailBody, objectKeyPattern);
            var objectType = ExtractValue(emailBody, objectTypePattern);
            var lastModified = ExtractValue(emailBody, lastModifiedPattern);
            var objectSize = ExtractValue(emailBody, objectSizePattern);

            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(eventType, expectedEventType,
                    "Verify metadata 'event_type' is as expected"),
                () => AssertHelper.AreEquals(objectKey, expectedImageDto.ObjectKey,
                    "Verify metadata 'object_key' is as expected"),
                () => AssertHelper.AreEquals(objectType, expectedImageDto.ObjectType,
                    "Verify metadata 'object_type' is as expected"),
                () => AssertHelper.AreEquals(int.Parse(objectSize), expectedImageDto.ObjectSize,
                    "Verify metadata 'object_size' is as expected")
            );
        }

        private static string ExtractValue(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value.Replace("\r", "").Replace("\n", "") : string.Empty;
        }

        private void ConfirmSubscriptionViaConfirmationUrlSentByEmail(string emailBody)
        {
            //extract the confirmation url from email Body
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(emailBody);
            var confirmLinkNode =
                htmlDoc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Confirm subscription')]");
            var hrefValue = confirmLinkNode?.GetAttributeValue("href", string.Empty);

            //api action: confirm subscription by obtained url from confirmation email
            //1st GET: https://{host}/confirmation.html?TopicArn={topicArn}?Token={token}?Endpoint={endpoint}
            var getRequest = new RestRequest(hrefValue);
            var getResponse = MyRestClient.Execute(getRequest);
            AssertHelper.IsTrue(getResponse.IsSuccessStatusCode,
                $"Verify GET {getResponse.ResponseUri} has 'Success' status code and user subscribed successfully");

            //2nd GET: https://{host}/?Action=ConfirmSubscription?TopicArn={topicArn}?Token={token}
            var uri = new Uri(hrefValue);
            //parse the query string
            var queryParameters = HttpUtility.ParseQueryString(uri.Query);
            //retrieve the TopicArn and Token parameters
            var topicArn = queryParameters["TopicArn"];
            var token = queryParameters["Token"];
            var host = uri.Host;
            getRequest = new RestRequest($"https://{host}/?Action=ConfirmSubscription&TopicArn={topicArn}&Token={token}");
            getResponse = MyRestClient.Execute(getRequest);
            AssertHelper.IsTrue(getResponse.IsSuccessStatusCode,
                $"Verify GET {getResponse.ResponseUri} has 'Success' status code and user subscribed successfully");
        }

        [TearDown]
        protected void AfterEach()
        {
            //delete all emails in test mailbox
            MailslurpClient.DeleteAllInboxEmails(Guid.Parse(InboxId));
            try
            {
                var deleteRequest = new RestRequest($"{NotificationApiEndpoint}/{UserEmail}", Method.Delete);
                MyRestClient.Execute(deleteRequest);
            }
            catch (Exception ex)
            {
                Log.Error("Some Error occured in TearDown:" + ex.Message);
            }
        }
    }
}
