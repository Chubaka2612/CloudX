using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace CloudX.Auto.AWS.Core.Domain.SLESS
{
    public class CloudWatchLogsService
    {
        private static CloudWatchLogsService _instance;

        private readonly IAmazonCloudWatchLogs _cloudWatchService;

        private static readonly object Padlock = new object();

        private static RegionEndpoint _region;

        private CloudWatchLogsService()
        {
            _cloudWatchService = new AmazonCloudWatchLogsClient(_region);
        }

        public IAmazonCloudWatchLogs GetClient()
        {
            return _cloudWatchService;
        }

        public static CloudWatchLogsService Instance(RegionEndpoint region = default)
        {
            if (_instance == null || _region != region)
            {
                lock (Padlock)
                {
                    if (_instance == null || _region != region)
                    {
                        _region = region;
                        _instance = new CloudWatchLogsService();
                    }
                }
            }
            return _instance;
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

        public async Task<List<LogStream>> GetCloudWatchLogStreamsAsync(string logGroupName, string instanceId)
        {
            var describeLogStreamsRequest = new DescribeLogStreamsRequest
            {
                LogGroupName = logGroupName,
                LogStreamNamePrefix = instanceId
            };
            var describeLogStreamsResponse = await _cloudWatchService.DescribeLogStreamsAsync(describeLogStreamsRequest);

            return describeLogStreamsResponse.LogStreams;
        }

        public async Task<List<LogStream>> GetCloudWatchLogStreamsAsync(string logGroupName)
        {
            var describeLogStreamsRequest = new DescribeLogStreamsRequest
            {
                LogGroupName = logGroupName,
            };
            var describeLogStreamsResponse = await _cloudWatchService.DescribeLogStreamsAsync(describeLogStreamsRequest);

            return describeLogStreamsResponse.LogStreams;
        }

        public async Task<List<string>> GetLogGroupsByPrefixAsync(string prefix)
        {
            var logGroups = new List<string>();
            string nextToken = null;

            do
            {
                var request = new DescribeLogGroupsRequest
                {
                    LogGroupNamePrefix = prefix,
                    NextToken = nextToken
                };

                var response = await _cloudWatchService.DescribeLogGroupsAsync(request);
                nextToken = response.NextToken;

                logGroups.AddRange(response.LogGroups.Select(lg => lg.LogGroupName));
            } while (nextToken != null);

            return logGroups;
        }


        public async Task<List<FilteredLogEvent>> FilterLogEventsAsync(string logGroupName, string messagePrefix, DateTime startTime, DateTime endTime)
        {
            var filterLogEventsResponse = await _cloudWatchService.FilterLogEventsAsync(new FilterLogEventsRequest
            {
                LogGroupName = logGroupName,
                StartTime = ((DateTimeOffset)startTime).ToUnixTimeMilliseconds(),
                EndTime = ((DateTimeOffset)endTime).ToUnixTimeMilliseconds(),
                FilterPattern = $"%{messagePrefix}%"
            });

            return filterLogEventsResponse.Events;
        }
    }
}