using System.Linq;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Tests.TestData.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.Core.Utils;

namespace CloudX.Auto.Tests.EC2.Deployment
{
    public class EC2FunctionalValidationTest : BaseTest
    {
        private const string testDataFilePath = "EC2\\ec2_test_data.json";

        [Test]
        [Component(ComponentName.CloudX_EC2)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-EC2-04")]
        public void ApplicationApiEndpointShouldRespondWithCorrectInformation()
        {
            var port = 80;
            var publicInstanceTestData = ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
                testDataFilePath).EC2Instances.Where(instance => instance.IsPublic).First();

            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                 .First(instance => instance.InstanceId == publicInstanceTestData.Id);

            var client = new RestClient($"http://{publicInstanceTestData.Ip}:{port}");
            var request = new RestRequest("/", Method.Get);
            var response = client.Execute(request);

            dynamic jsonResponse = JObject.Parse(response.Content);
            string availabilityZone = jsonResponse.availability_zone;
            string privateIpv4 = jsonResponse.private_ipv4;
            string region = jsonResponse.region;

            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(availabilityZone, instance.Placement.AvailabilityZone, "Verify availability zone is correct for the instance"),
                () => AssertHelper.AreEquals(privateIpv4, instance.PrivateIpAddress, "Verify private ip4 is correct for the instance"),
                () => AssertHelper.AreEquals(region, instance.Placement.AvailabilityZone.Substring(0,
                instance.Placement.AvailabilityZone.Length - 1), "Verify regionis correct for the instance")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_EC2)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-EC2-03")]
        public void InstanseShouldNotBeAvailableFromPort()
        {
            var wrongPort = 90;
            var publicInstanceTestData = ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
                testDataFilePath).EC2Instances.Where(instance => instance.IsPublic).First();

            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                 .First(instance => instance.InstanceId == publicInstanceTestData.Id);

            var client = new RestClient($"http://{publicInstanceTestData.Ip}:{wrongPort}");
            var request = new RestRequest("/", Method.Get);
            var response = client.Execute(request);
            AssertHelper.IsFalse(response.IsSuccessful, "Veriy API call could not be made to public instance with wrong port");
        }
    }
}