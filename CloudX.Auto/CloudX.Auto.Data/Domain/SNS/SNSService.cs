using System;
using System.Collections.Generic;
using Amazon.SimpleNotificationService;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace CloudX.Auto.AWS.Core.Domain.SNS
{
    public class SNSService
    {
        private static SNSService _instance;

        private readonly IAmazonSimpleNotificationService _snsService;

        private static readonly object Padlock = new object();

        private SNSService()
        {
            _snsService = new AmazonSimpleNotificationServiceClient();
        }

        public IAmazonSimpleNotificationService GetClient()
        {
            return _snsService;
        }

        public static SNSService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SNSService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<ListTopicsResponse> ListTopicsAsync()
        {
            ListTopicsResponse listTopicsResponse;
            try
            {
                listTopicsResponse = await _snsService.ListTopicsAsync();
            }
            catch
            {
                throw new Exception("Can't obtain SNS topics");
            }
            return listTopicsResponse;
        }

        public async Task<Dictionary<string, string>> GetTopicAttributesAsync(string topicArn)
        {
            GetTopicAttributesResponse attributesResponse;
            try
            {
                attributesResponse = await _snsService.GetTopicAttributesAsync(new GetTopicAttributesRequest
                {
                    TopicArn = topicArn
                });
            }
            catch
            {
                throw new Exception("Can't obtain SNS attributes");
            }
            return attributesResponse.Attributes;
        }

        public async Task<List<Tag>> GetTopicTagsAsync(string topicArn)
        {
            ListTagsForResourceResponse tagsResponse;
            try
            {
                tagsResponse = await _snsService.ListTagsForResourceAsync(new ListTagsForResourceRequest
                {
                    ResourceArn = topicArn
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