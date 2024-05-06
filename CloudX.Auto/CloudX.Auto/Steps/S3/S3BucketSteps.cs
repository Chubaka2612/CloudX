using Amazon.S3.Model;
using CloudX.Auto.AWS.Core.Domain.S3;
using log4net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudX.Auto.Tests.Steps.S3
{
    public static class S3BucketSteps
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(S3BucketSteps));

        public static async Task<List<S3Object>> ListS3ObjectsByKey(this S3Service s3Service, string bucketId, string key)
        {
            Log.Info($"Get all objects from s3 bucket: {bucketId}");

            var response = await s3Service.GetClient().ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketId,
                Prefix = key
            });

            return response.S3Objects;
        }


        public static async Task<GetObjectMetadataResponse> ListS3ObjectsMetaDataByKey(this S3Service s3Service, string bucketId, string key)
        {
            Log.Info($"Get object's meta by key: {key} from s3 bucket: {bucketId}");

            var response = await s3Service.GetClient().GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucketId,
                Key = key
            });
            return response;
        }
    }
}