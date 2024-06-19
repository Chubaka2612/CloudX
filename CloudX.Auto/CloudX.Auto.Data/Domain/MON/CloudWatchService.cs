using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace CloudX.Auto.AWS.Core.Domain.MON
{
    public class CloudWatchService
    {
        private static CloudWatchService _instance;

        private readonly IAmazonCloudWatch _cloudWatchService;

        private static readonly object Padlock = new object();

        private CloudWatchService()
        {
            _cloudWatchService = new AmazonCloudWatchClient();
        }

        public IAmazonCloudWatch GetClient()
        {
            return _cloudWatchService;
        }

        public static CloudWatchService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CloudWatchService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<List<Metric>> GetCloudWatchMetricsAsync(string instanceId)
        {
            var response = await _cloudWatchService.ListMetricsAsync(new ListMetricsRequest
            {
                Dimensions = new List<DimensionFilter>
            {
                new DimensionFilter
                {
                    Name = "InstanceId",
                    Value = instanceId
                }
            }
            });
            return response.Metrics;
        }
    }
}