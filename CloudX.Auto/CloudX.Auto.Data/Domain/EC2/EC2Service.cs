using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace CloudX.Auto.AWS.Core.Domain.EC2
{
    public class EC2Service
    {
        private static EC2Service _instance;

        private readonly IAmazonEC2 _ec2Service;
        
        private static readonly object Padlock = new object();

        private EC2Service()
        {
            _ec2Service = new AmazonEC2Client();
        }

        public static EC2Service Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new EC2Service();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<List<Instance>> ListInstancesAsync()
        {
            var instances = new List<Instance>();
            var paginator = _ec2Service.Paginators.DescribeInstances(new DescribeInstancesRequest());

            await foreach (var response in paginator.Responses)
            {
                foreach (var reservation in response.Reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        instances.Add(instance);
                    }
                }
            }
            return instances;
        }

        public async Task<DescribeVolumesResponse> GetVolumeAsync(string volumeId)
        {
            var describeVolumesRequest = new DescribeVolumesRequest
            {
                VolumeIds = new List<string> { volumeId }
            };

            return await _ec2Service.DescribeVolumesAsync(describeVolumesRequest);
        }

        public async Task<List<SecurityGroup>> ListSecurityGroupsAsync(string groupId)
        {
            var request = new DescribeSecurityGroupsRequest
            {
                GroupIds = new List<string> { groupId }
            };

            var response = await _ec2Service.DescribeSecurityGroupsAsync(request);
            return response.SecurityGroups;
        }

        public async Task<List<Vpc>> ListVpcsAsync(string vpcId)
        {
            var request = new DescribeVpcsRequest
            {
                VpcIds = new List<string> { vpcId }
            };

            var response = await _ec2Service.DescribeVpcsAsync(request);
            return response.Vpcs;
        }

        public async Task<List<Subnet>> ListVpcSubnetsAsync(string vpcId)
        {
            var request = new DescribeSubnetsRequest
            {
                Filters = new List<Filter>
                {
                    new Filter { Name = "vpc-id", Values = new List<string> { vpcId } }
                }
            };

            var response = await _ec2Service.DescribeSubnetsAsync(request);
            return response.Subnets;
        }

        public async Task<List<Subnet>> ListVpcSubnetsByInstanceIdAsync(string instanceId)
        {
            var describeNetworkInterfacesRequest = new DescribeNetworkInterfacesRequest
            {
                Filters = new List<Filter>
                {
                    new Filter { Name = "attachment.instance-id", Values = new List<string> { instanceId } }
                }
            };

            var describeNetworkInterfacesResponse = await _ec2Service.DescribeNetworkInterfacesAsync(describeNetworkInterfacesRequest);

            // Get the subnet IDs associated with the network interfaces
            var subnetIds = describeNetworkInterfacesResponse.NetworkInterfaces.Select(ni => ni.SubnetId).ToList();

            // Describe the subnets using the subnet IDs
            var describeSubnetsRequest = new DescribeSubnetsRequest
            {
                Filters = new List<Filter>
                {
                    new Filter { Name = "subnet-id", Values = subnetIds }
                }
            };

            var describeSubnetsResponse = await _ec2Service.DescribeSubnetsAsync(describeSubnetsRequest);
            return describeSubnetsResponse.Subnets;
        }

        public async Task<List<RouteTable>> ListVpcSubnetsRoutTablesAsync(string subnetId)
        {
            var describeRouteTablesRequest = new DescribeRouteTablesRequest
            {
                Filters = new List<Filter>
                        {
                            new Filter { Name = "association.subnet-id", Values = new List<string> { subnetId } }
                        }
            };

            var response = await _ec2Service.DescribeRouteTablesAsync(describeRouteTablesRequest);
            return response.RouteTables;
        }
    }
}
