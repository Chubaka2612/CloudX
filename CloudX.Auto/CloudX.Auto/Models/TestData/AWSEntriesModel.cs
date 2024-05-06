using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class AWSEntriesModel
    {
        public AWSEntriesModel()
        {
            AWSEntries = new List<AWSEntryModel>();
        }

        public List<AWSEntryModel> AWSEntries { get; set; }
    }
}
