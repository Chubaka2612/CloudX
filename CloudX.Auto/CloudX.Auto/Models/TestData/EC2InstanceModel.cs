using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class EC2InstanceModel
    {
        public EC2InstanceModel()
        {
            Tags = new List<string>();
        }

        public string Ip { get; set; }

        public string Id { get; set; }

        public string Type { get; set; }

        public int DeviceSizeGb { get; set; }

        public string OS { get; set; }

        public bool IsPublic { get; set; }

        public List<string> Tags { get; set; }
    }
}
