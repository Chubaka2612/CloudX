
using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class SnsTopic
    {
        public SnsTopic()
        {
            Tags = new List<string>();
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public bool Encryption { get; set; }

        public List<string> Tags { get; set; }
    }
}
