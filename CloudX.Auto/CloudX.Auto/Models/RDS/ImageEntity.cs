using System;

namespace CloudX.Auto.Tests.Models.RDS
{
    public class ImageEntity: BaseEntity
    {
        public string ObjectKey { get; set; }

        public string ObjectType { get; set; }
        
        public DateTime LastModified { get; set; }
        
        public int ObjectSize { get; set; }

        public ImageEntity(string objectKey, string objectType, DateTime lastModified, int objectSize)
        {
            ObjectKey = objectKey;
            ObjectType = objectType;
            LastModified = lastModified;
            ObjectSize = objectSize;
        }

        public ImageEntity() { }
    }
}
