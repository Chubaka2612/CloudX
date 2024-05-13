using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class RDSInstanceModel
    {
        public RDSInstanceModel()
        {
            Tags = new List<string>();
        }

        public string Identifier { get; set; }

        public string Type { get; set; }

        public bool IsMultiAZ { get; set; }
        
        public int StorageSize { get; set; }

        public string StorageType { get; set; }

        public bool Encryption { get; set; }

        public string DatabaseType { get; set; }

        public string DatabaseVersion { get; set; }

        public List<string> Tags { get; set; }
    }
}
