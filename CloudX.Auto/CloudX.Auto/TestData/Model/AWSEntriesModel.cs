using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
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
