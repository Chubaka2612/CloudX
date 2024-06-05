using Newtonsoft.Json;

namespace CloudX.Auto.Tests.Dto
{
    public class ImageDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("last_modified")]
        public double LastModified { get; set; }

        [JsonProperty("object_key")]
        public string ObjectKey { get; set; }

        [JsonProperty("object_size")]
        public double ObjectSize { get; set; }

        [JsonProperty("object_type")]
        public string ObjectType { get; set; }
    }
}
