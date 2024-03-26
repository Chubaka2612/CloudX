using System.Collections.Generic;
using CloudX.Auto.AWS.Core.Converters;
using Newtonsoft.Json;

namespace CloudX.Auto.AWS.Core.Domain.IAM.Dto
{
    public class PolicyDocumentDto: AbstractDto
    {
        [JsonProperty("Version")] 
        public string Version { get; set; }
        
        [JsonProperty("Statement")] 
        public List<Statement> Statement { get; set; }
    }

    public class Statement
    {
        [JsonProperty("Action")]
        [JsonConverter(typeof(StringToListOfStringConverter))]
        public List<string> Action { get; set; }

        [JsonProperty("Resource")]
        public string Resource { get; set; }

        [JsonProperty("Effect")]
        public string Effect { get; set; }
    }
}
