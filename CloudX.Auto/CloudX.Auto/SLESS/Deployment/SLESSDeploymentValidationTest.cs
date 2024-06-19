
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CloudX.Auto.AWS.Core.Domain.IAM;
using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using CloudX.Auto.AWS.Core.Domain.SLESS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.SLESS.Deployment
{
    public class SLESSDeploymentValidationTest : ImageBaseTest
    {
        private const string SLESSTestDataFilePath = "SLESS\\sless_test_data.json";
        private JsonObject properties;

        [SetUp]
        protected void BeforeEach()
        {
            properties = ReadConfig(SLESSTestDataFilePath);

            Log.Debug("Initialize Rest client");
            var publicIp = ConfigurationManager.GetConfiguration(SLESSTestDataFilePath)["PublicIP"];
            ImageApiEndpoint = ConfigurationManager.GetConfiguration(SLESSTestDataFilePath)["BaseImageApiEndpoint"];
            MyRestClient = new RestClient($"http://{publicIp}");
        }

        [Test]
        [Component(ComponentName.CloudX_SLESS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SLESS-01")]
        public async Task DynamoDbTableRequirementsShouldBeCorrect()
        {
            #region Properties

            var tablePrefix = properties["DynamoDb"]["Name"].ToString();
            var secondaryIndexEnabled = (bool)properties["DynamoDb"]["SecondaryIndexEnabled"];
            var readCapacityUnits = (int)properties["DynamoDb"]["ReadCapacityUnits"];
            var writeCapacityUnits = (int)properties["DynamoDb"]["WriteCapacityUnits"];
            var expectedTimeToLive = properties["DynamoDb"]["TimeToLive"].ToString();
            var tags = properties["DynamoDb"]["Tags"].AsArray().Select(t => t.ToString()).ToArray();

            #endregion

            var table =
                await DynamoDbService.Instance.GetDynamoDbTableDescriptionAsync(tablePrefix);
            var readCapacityMatches = table.ProvisionedThroughput.ReadCapacityUnits;
            var writeCapacityMatches = table.ProvisionedThroughput.WriteCapacityUnits;
            var ttlResponse = await DynamoDbService.Instance.GetClient().DescribeTimeToLiveAsync(
                new DescribeTimeToLiveRequest
                {
                    TableName = table.TableName
                });

            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(
                    table.GlobalSecondaryIndexes != null && table.GlobalSecondaryIndexes.Count > 0,
                    secondaryIndexEnabled,
                    "Verify Global secondary indexes: not enabled"),
                () => AssertHelper.AreEquals(readCapacityMatches, readCapacityUnits,
                    "Verify Provisioned read capacity units: 5 (autoscaling for reads: Off)"),
                () => AssertHelper.IsTrue(
                    writeCapacityMatches >= writeCapacityUnits && writeCapacityMatches <= readCapacityUnits,
                    "Provisioned write capacity units: 5 (autoscaling for writes: On, 1-5 units)"),
                () => AssertHelper.IsTrue(
                    ttlResponse.TimeToLiveDescription.TimeToLiveStatus == TimeToLiveStatus.DISABLED &&
                    expectedTimeToLive == TimeToLiveStatus.DISABLED.Value, "Verify Time to Live: disabled"),
                () => AssertHelper.IsTrue(
                    tags.All(tag =>
                        DynamoDbService.Instance.GetDynamoDbTableTagsAsync(table.TableArn).Result
                            .Any(t => $"{t.Key}:{t.Value}" == tag)), "Verify tags are correct")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_SLESS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SLESS-02")]
        public async Task DynamoDbTableAttributesDefinitionShouldBeCorrect()
        {
            var tablePrefix = properties["DynamoDb"]["Name"].ToString();

            //api action: upload
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "SLESS\\Resources";

            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            MyRestClient.Execute(getRequest);

            var scanResponse = await DynamoDbService.Instance.ScanTable(tablePrefix);
            var requiredAttributes = new List<string>
            {
                "object_key",
                "object_type",
                "object_size",
                "created_at",
                "id",
                "last_modified"
            };

            var itemsWithMissingAttributes = scanResponse.Items
                .Where(item => !requiredAttributes.All(attribute => item.ContainsKey(attribute)))
                .ToList();

            if (itemsWithMissingAttributes.Any())
            {
                foreach (var item in itemsWithMissingAttributes)
                {
                    var missingAttributes = requiredAttributes
                        .Where(attribute => !item.ContainsKey(attribute))
                        .ToList();
                    Log.Info(
                        $"Item with id: {item["id"].S} is missing attributes: {string.Join(", ", missingAttributes)}");
                }
            }

            AssertHelper.IsFalse(itemsWithMissingAttributes.Any(),
                "Verify Dynamo Db Table has correct attributes definition");
        }

        [Test]
        [Component(ComponentName.CloudX_SLESS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SLESS-06")]
        public async Task ApplicationShouldHaveAccessToRequiredResources()
        {
            var roleName = "cloudxserverless-AppInstanceInstanceRole";
            var expectedPolicies = new List<string> { "s3:", "sns:", "dynamodb:" };
            var attachedPolicies = await IAMService.Instance.ListAttachedRolePoliciesAsync(roleName);
            var inlinePolicies = await IAMService.Instance.ListInlineRolePoliciesAsync(roleName);
            var policyDocDtos = new List<PolicyDocumentDto>();

            foreach (var policy in attachedPolicies)
            {
                var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.PolicyArn);
                var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.PolicyArn, version);
                var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);
                policyDocDtos.Add(policyDocDto);
            }

            foreach (var policy in inlinePolicies)
            {
                var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policy);
                policyDocDtos.Add(policyDocDto);
            }

            foreach (var expectedPolicy in expectedPolicies)
            {
                var policyStatements = policyDocDtos.Select(p => p.Statement.First()).ToList();
                var isPolicyPresent = policyStatements.Any(statement =>
                    statement.Resource.Any(r => r.Contains(expectedPolicy))
                    && statement.Effect == "Allow");
                AssertHelper.IsTrue(isPolicyPresent, $"Policy {expectedPolicy} is present");
            }
        }

        [Test]
        [Component(ComponentName.CloudX_SLESS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-SLESS-06")]
        public async Task LambdaRequirementsShouldBeCorrect()
        {
            #region Properties

            var lambdaConfig = properties["LambdaConfiguration"];
            var memory = int.Parse(lambdaConfig["Memory"].ToString());
            var ephemeralStorage = int.Parse(lambdaConfig["EphemeralStorage"].ToString());
            var timeout = int.Parse(lambdaConfig["Timeout"].ToString());
            var tags = lambdaConfig["Tags"].AsArray().Select(t => t.ToString()).ToArray();

            #endregion

            var lambdaFunction =
                await LambdaService.Instance.GetLambdaFunctionConfigurationAsync("cloudxserverless-EventHandlerLambda");
            var lambdaEventSourceMappings = await LambdaService.Instance.GetLambdaEventSourceMappingsAsync();

            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(lambdaFunction.MemorySize, memory, "Verify Lambda Memory Size is 128 MB"),
                () => AssertHelper.AreEquals(lambdaFunction.EphemeralStorage.Size, ephemeralStorage,
                    "Verify Lambda Ephemeral Storage not 512 MB"),
                () => AssertHelper.AreEquals(lambdaFunction.Timeout, timeout, "Verify Lambda Timeout not 3 seconds"),
                () => AssertHelper.IsTrue(
                    tags.All(tag =>
                        LambdaService.Instance.GetLambdaFunctionTagsAsync(lambdaFunction.FunctionArn).Result
                            .Any(t => $"{t.Key}:{t.Value}" == tag)), "Verify tags are correct"),
                () => AssertHelper.IsTrue(
                    lambdaEventSourceMappings.Any(mapping =>
                        mapping.EventSourceArn.Contains("cloudxserverless-QueueSQSQueue")),
                    "Verify Lambda Trigger: SQS Queue"),
                () => AssertHelper.IsNotNull(
                    CloudWatchLogsService.Instance().GetCloudWatchLogGroupAsync(
                        "/aws/lambda/cloudxserverless-EventHandlerLambda"),
                    "Verify Lambda application logs are stored in CloudWatch log group ")
            );
        }
    }
}
