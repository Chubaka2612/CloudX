using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.EC2;
using CloudX.Auto.AWS.Core.Domain.RDS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Models.TestData;
using NUnit.Framework;

namespace CloudX.Auto.Tests.RDS.Deployment
{
    public class RDSDeploymentValidationTest : BaseTest
    {

        private const string rdsTestDataFilePath = "RDS\\rds_test_data.json";

        protected static RDSInstanceModel sourceRdsInstance = ConfigurationManager.Get<RDSInstancesModel>(nameof(RDSInstancesModel),
               rdsTestDataFilePath).RDSInstances.First();

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-01")]
        public async Task RDSInstanceInternetAccessebilityShouldBeCorrect()
        {
            var dbInstance = RDSService.Instance.DescribeDBInstancesAsync(sourceRdsInstance.Identifier).Result.DBInstances.First();
            AssertHelper.IsFalse(dbInstance.PubliclyAccessible, "Verify db instance is not publicly accessible");

            // Get the subnet group associated with the RDS instance
            var dbSubnetGroupName = dbInstance.DBSubnetGroup?.DBSubnetGroupName;
            AssertHelper.IsNotNull(dbSubnetGroupName, "Verift db instance has Subnet Group associated");
            
            var subnetsAssignedToDbInstanceIdentifiers = RDSService.Instance.DescribeDBSubnetGroupsAsync(dbSubnetGroupName).Result.DBSubnetGroups.First().Subnets.Select(subnet => subnet.SubnetIdentifier);

            foreach (var subnetId in subnetsAssignedToDbInstanceIdentifiers)
            {
                var routeTables = await EC2Service.Instance.ListSubnetsRoutTablesAsync(subnetId);
                // Check if any of the route tables contain a route to an Internet Gateway
                bool isPublic = routeTables.Any(routeTable =>
                         routeTable.Routes.Any(route => route.GatewayId != null && route.GatewayId.StartsWith("igw-")));
                AssertHelper.IsFalse(isPublic, "Verify subnet of the dbInstance is not public");
            }

            var vpcId = dbInstance.DBSubnetGroup?.VpcId;
            AssertHelper.IsNotNull(dbSubnetGroupName, "Verift db instance has VPC associated");

            //Select non-default securityGroups belomg to dbInstance VPC
            var securityGroupsDetails = EC2Service.Instance.ListSecurityGroupsAsync().Result.Where(securityGroup => securityGroup.VpcId == vpcId)
                .Where(securityGroup => securityGroup.GroupName != "default");
            AssertHelper.AreEquals(securityGroupsDetails.Count(), 2, "Verify 2 Security Groups are associated with db instance");

            //Verify 'AppInstanceSecurityGroup'
            var appSecurityGroup = securityGroupsDetails.Where(securityGroup => securityGroup.GroupName.Contains("AppInstanceSecurityGroup")).First();
            bool allowsPublicInbound = appSecurityGroup.IpPermissions.Any(inboundRule => inboundRule.Ipv4Ranges.Count > 0 && inboundRule.Ipv4Ranges[0].CidrIp == "0.0.0.0/0");
            AssertHelper.IsTrue(allowsPublicInbound, "Verify traffic of inbound rules of the 'AppInstanceSecurityGroup' security group is allowed");

            //Verify 'DatabaseMySQLInstanceSecurityGroup'
            var dbSecurityGroup = securityGroupsDetails.Where(securityGroup => securityGroup.GroupName.Contains("DatabaseMySQLInstanceSecurityGrou")).First();
            allowsPublicInbound = dbSecurityGroup.IpPermissions.Any(inboundRule => inboundRule.Ipv4Ranges.Count > 0 && inboundRule.Ipv4Ranges[0].CidrIp == "0.0.0.0/0");
            
            AssertHelper.AssertScope(
                () => AssertHelper.IsFalse(allowsPublicInbound, "Verify traffic of inbound rules of the 'DatabaseMySQLInstanceSecurityGroup' is not allowed"),
                () => AssertHelper.IsTrue(dbSecurityGroup.IpPermissions
                .Any(securityGroupRule => securityGroupRule.UserIdGroupPairs.Any(pair => pair.GroupId == appSecurityGroup.GroupId)),
                "Verify the 'DatabaseMySQLInstanceSecurityGroup' has rule of 'AppInstanceSecurityGroup' to allow inbound traffic: dbInstace is accessible only from the application's public subnet ")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-02")]
        public async Task RDSInstanceDeploymentRequiremnetsShouldBeCorrect()
        {
            var dbInstance = RDSService.Instance.DescribeDBInstancesAsync(sourceRdsInstance.Identifier).Result.DBInstances.First();

            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(dbInstance.DBInstanceClass, sourceRdsInstance.Type, "Verify db instance 'Type' is correct"),
                () => AssertHelper.AreEquals(dbInstance.MultiAZ, sourceRdsInstance.IsMultiAZ, "Verify db instance 'MultiAZ' is correct"),
                () => AssertHelper.AreEquals(dbInstance.AllocatedStorage, sourceRdsInstance.StorageSize, "Verify db instance 'Storage Size' is correct"),
                () => AssertHelper.AreEquals(dbInstance.StorageType, sourceRdsInstance.StorageType, "Verify db instance 'Storage Type' is correct"),
                () => AssertHelper.AreEquals(dbInstance.StorageEncrypted, sourceRdsInstance.Encryption, "Verify db instance 'Encription' is correct"),
                () => AssertHelper.AreEquals(dbInstance.Engine, sourceRdsInstance.DatabaseType, "Verify db instance 'DataBase Type' is correct"),
                () => AssertHelper.AreEquals(dbInstance.EngineVersion, sourceRdsInstance.DatabaseVersion, "Verify db instance 'DataBase Version' is correct"),
                () => AssertHelper.CollectionContains(dbInstance.TagList.Select(tag => tag.Key + ":" + tag.Value).ToList(), sourceRdsInstance.Tags,
                "Verify db instance contains expected 'Tags'")
                );
        }
    }
}
