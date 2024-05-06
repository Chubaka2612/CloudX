using System.Collections.Generic;
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
            var request = new DescribeSecurityGroupsRequest();
            var groupIds = new List<string> { groupId };
            request.GroupIds = groupIds;

            var response = await _ec2Service.DescribeSecurityGroupsAsync(request);
            return response.SecurityGroups;
        }
    }
}
