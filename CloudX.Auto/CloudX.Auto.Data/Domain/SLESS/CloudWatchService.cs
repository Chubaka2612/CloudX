using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace CloudX.Auto.AWS.Core.Domain.SLESS
{
    public class CloudWatchService
    {
        private static CloudWatchService _instance;

        private readonly IAmazonCloudWatchLogs _cloudWatchService;

        private static readonly object Padlock = new object();

        private CloudWatchService()
        {
            _cloudWatchService = new AmazonCloudWatchLogsClient();
        }

        public IAmazonCloudWatchLogs GetClient()
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

        public async Task<LogGroup> GetCloudWatchLogGroupAsync(string logGroupNamePrefix)
        {
            var request = new DescribeLogGroupsRequest
            {
                LogGroupNamePrefix = logGroupNamePrefix
            };

            var response = await _cloudWatchService.DescribeLogGroupsAsync(request);

            return response.LogGroups.Find(group => group.LogGroupName.StartsWith(logGroupNamePrefix));
        }
    }
}