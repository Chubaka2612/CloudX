using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
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
