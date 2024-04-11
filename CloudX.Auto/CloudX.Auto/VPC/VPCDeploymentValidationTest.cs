using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.TestData.Model;
using NUnit.Framework;

namespace CloudX.Auto.Tests.EC2.Deployment
{
    public class VPCDeploymentValidationTest : BaseTest
    {
        private const string vpcTestDataFilePath = "VPC\\vpc_test_data.json";
        private const string ec2TestDataFilePath = "EC2\\ec2_test_data.json";

        private readonly string vpcExpectedId = ConfigurationManager.GetConfiguration(vpcTestDataFilePath)["VpcId"];
        private readonly string vpcExpectedTags = ConfigurationManager.GetConfiguration(vpcTestDataFilePath)["VpcTags"];
        private readonly string vpcExpectedCidr = ConfigurationManager.GetConfiguration(vpcTestDataFilePath)["VpcCidrBlock"];

        [Test]
        [Component(ComponentName.CloudX_VPC)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-VPC-01")]
        public async Task VPCShouldHaveCorrectConfiguration()
        {
            var vpcActual = (await EC2Service.Instance.ListVpcsAsync(vpcExpectedId))
                .FirstOrDefault()
                         ?? throw new ArgumentException($"No vpc with id '{vpcExpectedId}' was found");

            AssertHelper.AssertScope(
                () => AssertHelper.IsFalse(vpcActual.IsDefault,
                "Verify vcp is not default"),
                () => AssertHelper.CollectionContains(vpcActual.Tags.Select(tag => tag.Key + ":" + tag.Value).ToList(), new string[] { vpcExpectedTags},
                "Verify tags of vpc are correct"),
                () => AssertHelper.AreEquals(vpcActual.CidrBlock, vpcExpectedCidr,
                "Verify CIDR of vpc is correct"));
        }

        [Test]
        [Component(ComponentName.CloudX_VPC)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-VPC-01")]
        public async Task VPCShouldHaveCorrectSubnets()
        {
            var vpcSubnets = (await EC2Service.Instance.ListVpcSubnetsAsync(vpcExpectedId));
            AssertHelper.IsTrue(vpcSubnets.Count() == 2, $"Verify subnets for vpc with id {vpcExpectedId} are present");

            AssertHelper.AssertScope(
                () => AssertHelper.IsNotNull(vpcSubnets.FirstOrDefault(subnet => subnet.MapPublicIpOnLaunch),
                "Verify vcp has public subnet"),
                () => AssertHelper.IsNotNull(vpcSubnets.FirstOrDefault(subnet => !subnet.MapPublicIpOnLaunch),
                "Verify vcp has private subnet")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_VPC)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-VPC-02")]
        public async Task VPCPublicInstanceShouldBeAccessibleFromTheInternetByInternetGateway()
        {
            var publicInstanceId = ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
              ec2TestDataFilePath).EC2Instances.Where(instance => instance.IsPublic).First().Id;

            var vpcSubnets = await EC2Service.Instance.ListVpcSubnetsByInstanceIdAsync(publicInstanceId);

            AssertHelper.AssertScope(
                () => AssertHelper.IsTrue(vpcSubnets.Count() == 1, $"Verify the public instance has only 1 subnet"),
                () => AssertHelper.IsTrue(vpcSubnets.First().MapPublicIpOnLaunch, $"Verify the public instance has public subnet")
            );

            // Check the route table associated with the subnet where the EC2 instance is located.
            // If there is a route to the internet(0.0.0.0/0) pointing to an internet gateway,
            // then the EC2 instance has internet access.
            var routeTables = await EC2Service.Instance.ListVpcSubnetsRoutTablesAsync(vpcSubnets.First().SubnetId);
            Route igwRoute = null;
            routeTables.Select(routeTable => routeTable.Routes).ForEach(routs =>
            {
                 igwRoute = routs.FirstOrDefault(route => route.GatewayId.StartsWith("igw-"));
            });
            AssertHelper.IsNotNull(igwRoute, "Verify public instance has an internet gateway route");
            AssertHelper.IsTrue(igwRoute.DestinationCidrBlock.Contains("0.0.0.0/0"), "Verify the route has correct description");
        }

        [Test]
        [Component(ComponentName.CloudX_VPC)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-VPC-02")]
        public async Task VPCPrivateInstanceShouldHaveAccessToTheInternetViaNATGateway()
        {
            var privateInstanceId =  ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
              ec2TestDataFilePath).EC2Instances.Where(instance => !instance.IsPublic).First().Id; ;

            var vpcSubnets = await EC2Service.Instance.ListVpcSubnetsByInstanceIdAsync(privateInstanceId);

            AssertHelper.AssertScope(
                () => AssertHelper.IsTrue(vpcSubnets.Count() == 1, $"Verify the private instance has only 1 subnet"),
                () => AssertHelper.IsFalse(vpcSubnets.First().MapPublicIpOnLaunch, $"Verify the private instance has private subnet")
            );

            var routeTables = await EC2Service.Instance.ListVpcSubnetsRoutTablesAsync(vpcSubnets.First().SubnetId);
            Route natRoute = null;
            routeTables.Select(routeTable => routeTable.Routes).ForEach(routs =>
            {
                natRoute = routs.FirstOrDefault(route => route.NatGatewayId != null);
            });

            AssertHelper.AssertScope(
                () => AssertHelper.IsNotNull(natRoute, "Verify private instance has an NAT gateway route"),
                () => AssertHelper.IsNotNull(natRoute.NatGatewayId.Contains("nat-"), "Verify NAT gateway route has correct prefix")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_VPC)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-VPC-02")]
        public async Task VPCPrivateInstanceShouldNotBeAccessibleFromThePublicInternet()
        {
            var privateInstanceId = ConfigurationManager.Get<EC2InstancesModel>(nameof(EC2InstancesModel),
              ec2TestDataFilePath).EC2Instances.Where(instance => !instance.IsPublic).First().Id; ;
            var instance = EC2Service.Instance.ListInstancesAsync().Result.ToList()
                .FirstOrDefault(instance => instance.InstanceId == privateInstanceId)
                ?? throw new ArgumentException($"No instance with id '{privateInstanceId}' was found in InstanceList");

            //Check if the instance has a public IP address assigned.
            //This is a security concern as it bypasses NAT Gateway and exposes the instance directly to the internet.
            AssertHelper.IsFalse(instance.PublicIpAddress != null,
                $"Verify public ip address is not assigned on private instance");

            //Verify that the instance's security groups don't allow inbound traffic
            //from the public internet (0.0.0.0/0 CIDR block).
            var securityGroups = instance.SecurityGroups;
            bool allowsPublicInbound = false;

            foreach (var securityGroup in securityGroups)
            {
                var securityGroupDetails =  EC2Service.Instance.ListSecurityGroupsAsync(securityGroup.GroupId).Result.First();

                foreach (var inboundRule in securityGroupDetails.IpPermissions)
                {
                    if (inboundRule.Ipv4Ranges.Count > 0 && inboundRule.Ipv4Ranges[0].CidrIp == "0.0.0.0/0")
                    {
                        allowsPublicInbound = true;
                        break;
                    }
                }
            }

            AssertHelper.IsFalse(allowsPublicInbound, "Verify the private instance's security groups" +
                "restricts inbound traffic from the public internet");
        }
    }
}
