using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudTrail;
using Amazon.CloudTrail.Model;

namespace CloudX.Auto.AWS.Core.Domain.MON
{
    public class CloudTrailService
    {
        private static CloudTrailService _instance;

        private readonly IAmazonCloudTrail _cloudTrailService;

        private static readonly object Padlock = new object();

        private CloudTrailService()
        {
            _cloudTrailService = new AmazonCloudTrailClient();
        }

        public IAmazonCloudTrail GetClient()
        {
            return _cloudTrailService;
        }

        public static CloudTrailService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CloudTrailService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<Trail> GetCloudTrailDescription(string name)
        {
            try
            {
                var trailsResponse = await _cloudTrailService.DescribeTrailsAsync(new DescribeTrailsRequest
                {
                    IncludeShadowTrails = false
                });
                return trailsResponse.TrailList.Find(trail => trail.Name.StartsWith(name));
            }
            catch 
            {
                throw new Exception("Can't obtain trail description");
            }
        }

        public async Task<List<ResourceTag>> GetCloudTrailTagsAsync(string trailARN)
        {
            ListTagsResponse tagResponse;
            try
            {
                tagResponse = await _cloudTrailService.ListTagsAsync(new ListTagsRequest
                {
                    ResourceIdList = new List<string> { trailARN }
                });
            }
            catch
            {
                throw new Exception("Can't obtain trail attributes");
            }
            return tagResponse.ResourceTagList;
        }
    }
}