using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class PoliciesModel
    {
        public PoliciesModel()
        {
            Policies = new List<PolicyModel>();
        }

        public List<PolicyModel> Policies { get; set; }
    }
}
