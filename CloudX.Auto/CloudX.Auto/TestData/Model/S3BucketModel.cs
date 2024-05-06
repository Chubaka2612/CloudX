using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
{
    public class S3BucketModel
    {
        public S3BucketModel()
        {
            Tags = new List<string>();
        }

        public string Id { get; set; }

        public string EncryptionType { get; set; }

        public bool IsPublicAccess { get; set; }

        public string Versioning { get; set; }

        public List<string> Tags { get; set; }
    }
}
