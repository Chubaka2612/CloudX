using Amazon.RDS;
using Amazon.RDS.Model;
using System.Threading.Tasks;

namespace CloudX.Auto.AWS.Core.Domain.RDS
{
    public class RDSService
    {
        private static RDSService _instance;

        private readonly IAmazonRDS _rdsService;

        private static readonly object Padlock = new object();

        private RDSService()
        {
            _rdsService = new AmazonRDSClient();
        }

        public IAmazonRDS GetClient()
        {
            return _rdsService;
        }

        public static RDSService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new RDSService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<DescribeDBInstancesResponse> DescribeDBInstancesAsync(string instanceIdentifier)
        {
            DescribeDBInstancesResponse dbInstanceResponse;
            try
            {
                dbInstanceResponse = await _rdsService.DescribeDBInstancesAsync(new DescribeDBInstancesRequest
                {
                    DBInstanceIdentifier = instanceIdentifier
                });
            }
            catch
            {
                throw new DBInstanceNotFoundException($"Can't obtain db instance info by identifier: {instanceIdentifier}");
            }
            return dbInstanceResponse;
        }

        public async Task<DescribeDBSubnetGroupsResponse> DescribeDBSubnetGroupsAsync(string dbSubNetGroup)
        {
            DescribeDBSubnetGroupsResponse dbSubNetgroupResponse;
            try
            {
                dbSubNetgroupResponse = await _rdsService.DescribeDBSubnetGroupsAsync(new DescribeDBSubnetGroupsRequest
                {
                    DBSubnetGroupName = dbSubNetGroup
                });
            }
            catch
            {
                throw new DBSubnetGroupNotFoundException($"Can't obtain db instance Subnet Group by name: {dbSubNetGroup}");
            }
            return dbSubNetgroupResponse;
        }

        public async Task<DescribeDBSecurityGroupsResponse> DescribeDBSecurityGroupsAsync(string dbSecurityGroup)
        {
            DescribeDBSecurityGroupsResponse dbSecurityResponse;
            try
            {
                dbSecurityResponse = await _rdsService.DescribeDBSecurityGroupsAsync(new DescribeDBSecurityGroupsRequest
                {
                    DBSecurityGroupName = dbSecurityGroup
                });
            }
            catch
            {
                throw new DBSecurityGroupNotFoundException($"Can't obtain db instance Security Group by name: {dbSecurityGroup}");
            }
            return dbSecurityResponse;
        }


    }
}