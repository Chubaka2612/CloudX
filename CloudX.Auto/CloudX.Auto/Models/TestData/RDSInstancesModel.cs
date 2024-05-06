using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class RDSInstancesModel
    {
        public RDSInstancesModel()
        {
            RDSInstances = new List<RDSInstanceModel>();
        }

        public List<RDSInstanceModel> RDSInstances { get; set; }
    }
}
