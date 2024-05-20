using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.AWS.Core.Domain.IAM;
using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using CloudX.Auto.AWS.Core.Domain.S3;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Models.TestData;
using NUnit.Framework;
using Renci.SshNet;
using RestSharp;

namespace CloudX.Auto.Tests.S3.Deployment
{
    public class S3DeploymentValidationTest : BaseTest
    {

        private const string s3TestDataFilePath = "S3\\s3_test_data.json";

        protected static S3BucketModel sourceBucket = ConfigurationManager.Get<S3BucketsModel>(nameof(S3BucketsModel),
               s3TestDataFilePath).S3Buckets.First();

        protected string bucketId = $"cloudximage-imagestorebucket{sourceBucket.Id}";

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-02")]
        public async Task S3BucketRequiremnetsShouldBeCorrect()
        {
            var tags = S3Service.Instance.GetBucketTaggingAsync(bucketId).Result;
            var versioning = S3Service.Instance.GetBucketVersioningAsync(bucketId).Result;
            var encription = S3Service.Instance.GetBucketEncryptionAsync(bucketId).Result;
            var acl = S3Service.Instance.GetBucketACLAsync(bucketId).Result;

            var publicBlock = S3Service.Instance.GetPublicAccessBlockAsync(bucketId).Result;
            var encriptionAlgorithmToCheck = sourceBucket.EncryptionType == "SSE-S3" ? "AES256" : "AWSKMS";
            var policyStatusResponse = S3Service.Instance.GetBucketPolicyStatusAsync(bucketId).Result;
            AssertHelper.AssertScope(
                () => AssertHelper.CollectionContains(tags.Select(tag => tag.Key + ":" + tag.Value).ToList(), sourceBucket.Tags,
                "Verify tags of s3 are correct"),
                () => AssertHelper.AreEquals(versioning.Status.ToString(), sourceBucket.Versioning,
                "Verify versioning of s3 is correct"),
                () => AssertHelper.IsTrue(encription.ServerSideEncryptionRules.Any(rule => rule.ServerSideEncryptionByDefault?.ServerSideEncryptionAlgorithm?.Value == encriptionAlgorithmToCheck),
                "Verify encription type of s3 is correct: SSE-S3"),
                // Check for grants allowing public access
                () => AssertHelper.IsTrue(publicBlock.PublicAccessBlockConfiguration.BlockPublicPolicy,
                $"Verify Amazon should block access public policies for s3 bucket {bucketId}"),
                () => AssertHelper.IsFalse(policyStatusResponse.PolicyStatus.IsPublic || acl.Grants.Any(grant => grant.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers"),
                "Verify s3 bucket has no public access policy assigned")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-01")]
        public async Task S3ApplicationInstanceShouldBeAccessibleBySSHProtocol()
        {
            var hostname = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PublicIP"];
            var username = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["AwsUsername"];
            var privateKeyFilePath = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PrivateKeyFilePath"];
            var apiEndpoint = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BaseApiEndpoint"];
            var privateKeyFile = new PrivateKeyFile(privateKeyFilePath);
            
            using var client = new SshClient(hostname, username, privateKeyFile);
            //literally: ssh -i "cloudxinfo-eu-central-s3.pem" ec2-user@3.72.85.55
            client.Connect();// Attempt connection to instanse via ssh

            //check app is available by ssh protocol
            string curlCommand = $"curl -I http://{hostname}{apiEndpoint}";
            var commandResult = client.RunCommand(curlCommand);

            AssertHelper.IsTrue(commandResult.Result.Contains("HTTP/1.1 200 OK"));
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-01")]
        public async Task S3ApplicationShouldBeAccessiblePublicIP()
        {
            var publicIp = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PublicIP"];
            var apiEndpoint = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BaseApiEndpoint"];

            var client = new RestClient($"http://{publicIp}");
            var request = new RestRequest(apiEndpoint, Method.Get);
            var response = client.Execute(request);
            AssertHelper.IsTrue(response.IsSuccessful, $"Veriy application {response.ResponseUri} is accessible through public IP");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-01")]
        public async Task S3ApplicationShouldBeAccessibleFQDN()
        {
            var fdqn = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["FQDN"];
            var apiEndpoint = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BaseApiEndpoint"];

            var client = new RestClient($"http://{fdqn}");
            var request = new RestRequest(apiEndpoint, Method.Get);
         
            var response = client.Execute(request);
          
            AssertHelper.IsTrue(response.IsSuccessful, $"Verify application {response.ResponseUri} is accessible through FQDN");
        }


        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-01")]
        public async Task S3ApplicationShouldHaveAccessToS3BucketViaIAMRole()
        {
            //There isn't a direct way to check if an IAM role is "assigned" to an application itself
            //Applications running on EC2 instances often leverage Instance Profiles, which link an IAM role to the EC2 instance.

            var publicIp = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PublicIP"];

            var ec2Instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                 .First(instance => instance.PublicIpAddress == publicIp);

            //obtain iam profile name
            var iamProfileName = ec2Instance.IamInstanceProfile.Arn.Split('/')[1];
            var instanceProfileResponse = await IAMService.Instance.GetInstanceProfileAsync(iamProfileName);
            //obrain iam role name
            var iamRoleName = instanceProfileResponse.InstanceProfile.Roles[0].Arn.Split('/')[1];
            //obtain required policy
            var policy = IAMService.Instance.ListAttachedRolePoliciesAsync(iamRoleName)
              .Result.ToList().Where(policy => policy.PolicyName.Contains("cloudximage-ImageStoreBucketPolicy")).First();

            //obtain default version for a specific policy
            var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.PolicyArn);

            //obtain policy doc and map it to dto
            var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.PolicyArn, version);
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            //verify policy configuration
            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(policyDocDto.Statement.Count, 2, "Verify policy statement ammount is correct"),
                () => AssertHelper.AreEquals(policyDocDto.Statement.First().Effect, "Allow", "Verify policy statement[0] has correct Effect"),
                () => AssertHelper.AreEquals(policyDocDto.Statement.Last().Effect, "Allow", "Verify policy statement[1] has correct Effect"),
                () => AssertHelper.IsTrue(policyDocDto.Statement.First().Resource.Contains(bucketId), "Verify policy statement[0] has correct Resource"),
                () => AssertHelper.IsTrue(policyDocDto.Statement.Last().Resource.Contains(bucketId), "Verify policy statement[1] has correct Resource"),
                () => AssertHelper.CollectionEquals(policyDocDto.Statement.First().Action, new List<string>() { "s3:ListBucket" },
            "Verify policy statement[1] has correct 'Action' array"),
                () => AssertHelper.CollectionEquals(policyDocDto.Statement.Last().Action, new List<string>() { "s3:DeleteObject*", "s3:GetObject*", "s3:PutObject*" },
            "Verify policy statement[1] has correct 'Action' array") );
        }
    }
}
