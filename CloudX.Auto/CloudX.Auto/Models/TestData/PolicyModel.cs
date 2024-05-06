using System.Collections.Generic;

namespace CloudX.Auto.Tests.Models.TestData
{
    public class PolicyModel
    {
        public PolicyModel()
        {
            Action = new List<string>();
        }

        public string Name { get; set; }
        public List<string> Action { get; set; }
        public string Resource { get; set; }
        public string Effect { get; set; }
    }
}
