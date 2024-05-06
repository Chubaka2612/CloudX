using Newtonsoft.Json;
using System;

namespace CloudX.Auto.AWS.Core.Domain.IAM.Dto
{
    public class ImageDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("object_key")]
        public string ObjectKey { get; set; }

        [JsonProperty("object_size")]
        public int ObjectSize { get; set; }

        [JsonProperty("object_type")]
        public string ObjectType { get; set; }
    }
}
