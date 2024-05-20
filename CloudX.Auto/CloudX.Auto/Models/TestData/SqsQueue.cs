
using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class SqsQueue
    {
        public SqsQueue()
        {
            Tags = new List<string>();
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public bool Encryption { get; set; }

        public bool DeadLetterQueue { get; set; }

        public List<string> Tags { get; set; }
    }
}
