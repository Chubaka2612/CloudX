using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace CloudX.Auto.AWS.Core.Domain.SQS
{
    public class SQSService
    {
        private static SQSService _instance;

        private readonly IAmazonSQS _sqsService;

        private static readonly object Padlock = new object();

        private SQSService()
        {
            _sqsService = new AmazonSQSClient();
        }

        public IAmazonSQS GetClient()
        {
            return _sqsService;
        }

        public static SQSService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SQSService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<string> GetQueueUrlAsync(string queueName)
        {
            string queueUrl;
            try
            {
                var queuesResponse = await _sqsService.ListQueuesAsync(new ListQueuesRequest());
                queueUrl = queuesResponse.QueueUrls.First(entity => entity.Contains(queueName));
            }
            catch
            {
                throw new Exception("Can't obtain SQS Url");
            }
            return queueUrl;
        }

        public async Task<Dictionary<string, string>> GetQueueAttributesAsync(string queueUrl)
        {
            GetQueueAttributesResponse attributesResponse;
            try
            {
                attributesResponse = await _sqsService.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = new List<string> { "All" }
                });
            }
            catch
            {
                throw new Exception("Can't obtain SQS Queue attributes");
            }
            return attributesResponse.Attributes;
        }

        public async Task<Dictionary<string, string>> GetQueueTagsAsync(string queueUrl)
        {
            ListQueueTagsResponse tagsResponse;
            try
            {
                tagsResponse = await _sqsService.ListQueueTagsAsync(new ListQueueTagsRequest
                {
                    QueueUrl = queueUrl
                });
            }
            catch
            {
                throw new Exception("Can't obtain SNS attributes");
            }
            return tagsResponse.Tags;
        }
    }
}