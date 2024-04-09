using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.TestData.Model;
using NUnit.Framework;

namespace CloudX.Auto.Tests.EC2.Deployment
{
    public class EC2DeploymentValidationTest : BaseTest
    {
        private const string appName = "cloudxinfo";
        private const string testDataFilePath = "EC2\\ec2_test_data.json";
        protected static IEnumerable<TestCaseData> EC2InstanceTestDataSource()
        {
            var sourceList = ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
                testDataFilePath).EC2Instances;

            for (var i = 0; i < sourceList.Count; i++)
            {
                var tcd = new TestCaseData(sourceList.ElementAt(i));
                var prefix = sourceList.ElementAt(i).IsPublic ? "Public" : "Private";
                tcd.SetName("{m}[" + prefix + "]");
                yield return tcd;
            }
        }

        [Test]
        [Component(ComponentName.CloudX_EC2)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(EC2InstanceTestDataSource))]
        [TestCode("CXQA-EC2-01")]
        public async Task EC2InstancesShouldBeDeployed(EC2InstanceModel expectedInstanceTestDataModel)
        {
            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                             .First(instance => instance.InstanceId == expectedInstanceTestDataModel.Id);


            AssertHelper.NotNull(instance, $"Verify isntance with id '{expectedInstanceTestDataModel.Id}' is deployed");
        }

        [Test]
        [Component(ComponentName.CloudX_EC2)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(EC2InstanceTestDataSource))]
        [TestCode("CXQA-EC2-02")]
        public async Task EC2InstancesShouldHaveSpecificConfiguration(EC2InstanceModel expectedInstanceTestDataModel)
        {
            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                             .FirstOrDefault(instance => instance.InstanceId == expectedInstanceTestDataModel.Id)
                         ?? throw new ArgumentException(
                             $"No instance with id '{expectedInstanceTestDataModel.Id}' was found in InstanceList");

            //get volume of the instance
            var volume = EC2Service.Instance.GetVolumeAsync(instance.BlockDeviceMappings.Find(bdm => bdm.DeviceName == instance.RootDeviceName).Ebs.VolumeId);
            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(instance.InstanceType.Value, expectedInstanceTestDataModel.Type,
                "Verify type of instance is correct"),
                () => AssertHelper.CollectionContains(instance.Tags.Select(tag => tag.Key + ":" + tag.Value).ToList(), expectedInstanceTestDataModel.Tags,
                "Verify tags of instance are correct"),
                () => AssertHelper.AreEquals(instance.PlatformDetails, expectedInstanceTestDataModel.OS,
                "Verify platform details of instance are correct"),
                () => AssertHelper.AreEquals(volume.Result.Volumes[0].Size, expectedInstanceTestDataModel.DeviceSizeGb,
                "Verify Root Block Device Size of instance is correct"),
                () => AssertHelper.AreEquals(expectedInstanceTestDataModel.IsPublic, instance.PublicIpAddress != null,
                $"Verify public ip address is assigned: {expectedInstanceTestDataModel.IsPublic} on instanse isPublic: {expectedInstanceTestDataModel.IsPublic} ")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_EC2)]
        [TestCaseSource(nameof(EC2InstanceTestDataSource))]
        [Category(TestType.Regression)]
        [TestCode("CXQA-EC2-03")]
        public async Task EC2InstanceShouldHaveCorrectSecurityGroupConfigured(EC2InstanceModel expectedInstanceTestDataModel)
        {
            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                             .FirstOrDefault(instance => instance.InstanceId == expectedInstanceTestDataModel.Id)
                         ?? throw new ArgumentException(
                             $"No instance with id '{expectedInstanceTestDataModel.Id}' was found in InstanceList");

            //retrieve security group assigned to the public instance
            var securityGroupAssignedToInstance = instance.SecurityGroups.Where(group => group.GroupName.Contains(appName));
            AssertHelper.IsNotNull(securityGroupAssignedToInstance, "Verify security group is assigned to instance");

            //get security group description
            var securityGroupDescription = EC2Service.Instance.ListSecurityGroupsAsync(securityGroupAssignedToInstance.First()
                .GroupId).Result.First();
            bool sshAccessable = false;
            var httpAccessable = false;
            var ingressRules = securityGroupDescription.IpPermissions;
            foreach (var rule in ingressRules)
            {
                if (rule.IpProtocol == "tcp" && rule.FromPort <= 22 && rule.ToPort >= 22 && rule.Ipv4Ranges.Any(range => range.CidrIp == "0.0.0.0/0"))
                {
                    sshAccessable = true;
                    break;
                }
            }

            foreach (var rule in ingressRules)
            {
                if (rule.IpProtocol == "tcp" && rule.FromPort <= 80 && rule.ToPort >= 80 && rule.Ipv4Ranges.Any(range => range.CidrIp == "0.0.0.0/0"))
                {
                    httpAccessable = true;
                    break;
                }
            }

            AssertHelper.AssertScope(
                () => AssertHelper.IsTrue(httpAccessable, "Verify public instance is accessible from the internet HTTP (port 80)"),
                 () => AssertHelper.IsTrue(sshAccessable,
                "Verify public instance is accessible from the internet SSH (port 22)")
                );
        }
    }
}
