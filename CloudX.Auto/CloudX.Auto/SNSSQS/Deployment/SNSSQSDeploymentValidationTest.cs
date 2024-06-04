using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.AWS.Core.Domain.IAM;
using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using CloudX.Auto.AWS.Core.Domain.SNS;
using CloudX.Auto.AWS.Core.Domain.SQS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Models.TestData;
using NUnit.Framework;

namespace CloudX.Auto.Tests.SNSSQS.Deployment
{
    public class SNSSQSDeploymentValidationTest : BaseTest
    {
        private const string SnsSqsTestDataFilePath = "SNSSQS\\snssqs_test_data.json";
        protected static SnsTopic SourceSnsTopic = ConfigurationManager.Get<SnsTopic>(nameof(SnsTopic),
            SnsSqsTestDataFilePath);
        protected static SqsQueue SourceSqsQueue = ConfigurationManager.Get<SqsQueue>(nameof(SqsQueue),
            SnsSqsTestDataFilePath);

        [Test]
        [Component(ComponentName.CloudX_SNS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-01")]
        public async Task ApplicationInstanceRequirementsShouldBeCorrect()
        {
            var publicIp = ConfigurationManager.GetConfiguration(SnsSqsTestDataFilePath)["PublicIP"];
            var ec2Instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                .First(instance => instance.PublicIpAddress == publicIp);
            //obtain iam profile name
            var iamProfileName = ec2Instance.IamInstanceProfile.Arn.Split('/')[1];
            var instanceProfileResponse = await IAMService.Instance.GetInstanceProfileAsync(iamProfileName);
            //obtain iam role name
            var iamRoleName = instanceProfileResponse.InstanceProfile.Roles[0].Arn.Split('/')[1];
            var snsTopic = (await SNSService.Instance.ListTopicsAsync()).Topics.FirstOrDefault(topic => topic.TopicArn.Contains(SourceSnsTopic.Name))
                           ?? throw new Exception($"No SNS Topics with name '{SourceSnsTopic.Name}'");

            //Verify te application uses an SNS topic to subscribe and unsubscribe users, list existing subscriptions, and send e-mail
            //messages to subscribers 
            VerifyPolicyActions(iamRoleName, "cloudximage-TopicPublishPolicy", new List<string>{"sns:Publish"}, snsTopic.TopicArn);
            VerifyPolicyActions(iamRoleName, "cloudximage-TopicSubscriptionPolicy",
                new List<string> { "sns:ListSubscriptions*", "sns:Subscribe", "sns:Unsubscribe" }, snsTopic.TopicArn);
        }

        private void VerifyPolicyActions(string iamRoleName, string policyName, IList<string> expectedActions,
            string topicArn)
        {
            //obtain policies by IAM role
            var snsPolicyAttached =  IAMService.Instance.ListAttachedRolePoliciesAsync(iamRoleName).Result
                                    .FirstOrDefault(policy => policy.PolicyName.Contains(policyName))
                                    ?? throw new Exception($"Policy by name doesn't exist '{policyName}'");

            //obtain default version for a specific policy
            var version =  IAMService.Instance.GetDefaultPolicyVersionAsync(snsPolicyAttached.PolicyArn).Result;
            //obtain policy doc and map it to dto
            var policyDocJson =
                 IAMService.Instance.GetPolicyDocumentJsonAsync(snsPolicyAttached.PolicyArn, version).Result;
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            AssertHelper.AssertScope(
                () => AssertHelper.CollectionEquals(policyDocDto.Statement.First().Action, expectedActions,
                    $"Verify '{policyName}' has correct 'Action'"),
                () => AssertHelper.IsTrue(policyDocDto.Statement.First().Resource.Contains(topicArn))
            );
        }

        [Test]
        [Component(ComponentName.CloudX_SNS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-02")]
        public async Task SNSTopicRequirementsShouldBeCorrect()
        {
            //Check if SNS topic exists with specific name
            var snsTopic =  (await SNSService.Instance.ListTopicsAsync()).Topics.FirstOrDefault(topic => topic.TopicArn.Contains(SourceSnsTopic.Name))
                ?? throw new Exception( $"No SNS Topics with name '{SourceSnsTopic.Name}'");

            // Check if the topic is of type standard (not FIFO)
            AssertHelper.AreEquals(snsTopic.TopicArn.EndsWith(".fifo"), SourceSnsTopic.Encryption,"Verify the topic is of type standard, not FIFO.");

            // Check if encryption is disabled (encryption is disabled if the KmsMasterKeyId attribute is not set)
            var snsAttributes = await SNSService.Instance.GetTopicAttributesAsync(snsTopic.TopicArn);
            AssertHelper.AreEquals(snsAttributes.ContainsKey("KmsMasterKeyId"), SourceSnsTopic.Encryption,
                "Verify encryption is disabled");

            //Check tags
            var tags = await SNSService.Instance.GetTopicTagsAsync(snsTopic.TopicArn);
            AssertHelper.CollectionContains(tags.Select(tag => tag.Key + ":" + tag.Value).ToList(), SourceSnsTopic.Tags,
                "Verify tags of SNS topic are correct");
        }

        [Test]
        [Component(ComponentName.CloudX_SQS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SNSSQS-03")]
        public async Task SQSQueueRequirementsShouldBeCorrect()
        {
            //Check if SQS queue exists with specific name
            var sqsQueueUrl = await SQSService.Instance.GetQueueUrlAsync(SourceSqsQueue.Name);

            // Check if encryption is enabled 
            var sqsAttributes = await SQSService.Instance.GetQueueAttributesAsync(sqsQueueUrl);
            AssertHelper.AreEquals(bool.Parse(sqsAttributes["SqsManagedSseEnabled"]), SourceSqsQueue.Encryption, 
                "Check if encryption is enabled ");
            // Check if the sqs is of type standard (not FIFO)
            AssertHelper.IsFalse(sqsAttributes.TryGetValue("FifoQueue", out var fifoQueue) && fifoQueue == "true", "Verify the sqs is of type standard, not FIFO");

            //Check for Dead-letter queue
            AssertHelper.AreEquals(sqsAttributes.TryGetValue("RedrivePolicy", out var redrivePolicy), SourceSqsQueue.DeadLetterQueue, "Verify Dead-letter queue is correct");

            //Check tags
            var tags = await SQSService.Instance.GetQueueTagsAsync(sqsQueueUrl);
            AssertHelper.CollectionContains(tags.Select(tag => tag.Key + ":" + tag.Value).ToList(), SourceSnsTopic.Tags,
                "Verify tags of SQS Queue are correct");
        }
    }
}
