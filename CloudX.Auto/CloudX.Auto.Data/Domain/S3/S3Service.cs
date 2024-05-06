using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace CloudX.Auto.AWS.Core.Domain.S3
{
    public class S3Service
    {
        private static S3Service _instance;

        private readonly IAmazonS3 _s3Service;
        
        private static readonly object Padlock = new object();

        private S3Service()
        {
            _s3Service = new AmazonS3Client();
        }

        public IAmazonS3 GetClient() {
            return _s3Service;
        }

        public static S3Service Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new S3Service();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<List<Tag>> GetBucketTaggingAsync(string bucketName)
        {
            var taggingResponse = await _s3Service.GetBucketTaggingAsync(new GetBucketTaggingRequest
            {
                BucketName = bucketName
            });

          return taggingResponse.TagSet;
        }

        public async Task<S3BucketVersioningConfig> GetBucketVersioningAsync(string bucketName)
        {
            var versioningResponse = await _s3Service.GetBucketVersioningAsync(new GetBucketVersioningRequest
            {
                BucketName = bucketName
            });

            return versioningResponse.VersioningConfig;
        }

        public async Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(string bucketName)
        {
            var versioningResponse = await _s3Service.GetBucketEncryptionAsync(new GetBucketEncryptionRequest
            {
                BucketName = bucketName
            });

            return versioningResponse.ServerSideEncryptionConfiguration;
        }

        public async Task<S3AccessControlList> GetBucketACLAsync(string bucketName)
        {
            var aclResponse = await _s3Service.GetACLAsync(bucketName);

            return aclResponse.AccessControlList;
        }

        public async Task<GetBucketPolicyStatusResponse> GetBucketPolicyStatusAsync(string bucketName)
        {
            return await _s3Service.GetBucketPolicyStatusAsync(new GetBucketPolicyStatusRequest
            {
                BucketName = bucketName
            });
        }

        public async Task<GetPublicAccessBlockResponse> GetPublicAccessBlockAsync(string bucketName)
        {
            return await _s3Service.GetPublicAccessBlockAsync(new GetPublicAccessBlockRequest
            {
                BucketName = bucketName
            });
        }
    }
}
