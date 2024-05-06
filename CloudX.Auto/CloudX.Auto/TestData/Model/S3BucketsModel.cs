using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
{
    public class S3BucketsModel
    {
        public S3BucketsModel()
        {
            S3Buckets = new List<S3BucketModel>();
        }

        public List<S3BucketModel> S3Buckets { get; set; }
    }
}
