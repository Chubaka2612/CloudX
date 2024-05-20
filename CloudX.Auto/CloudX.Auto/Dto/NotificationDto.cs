using Newtonsoft.Json;

namespace CloudX.Auto.Tests.Dto
{
    public class NotificationDto
    {
        [JsonProperty("Endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("Owner")]
        public string Owner { get; set; }

        [JsonProperty("Protocol")]
        public string Protocol { get; set; }

        [JsonProperty("SubscriptionArn")]
        public string SubscriptionArn { get; set; }

        [JsonProperty("TopicArn")]
        public string TopicArn { get; set; }
    }
}
