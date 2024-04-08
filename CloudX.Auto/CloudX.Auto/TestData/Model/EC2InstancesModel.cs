using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using System.Collections.Generic;

namespace CloudX.Auto.Tests.TestData.Model
{
    public class EC2InstancesModel
    {
        public EC2InstancesModel()
        {
            EC2Instances = new List<EC2InstanceModel>();
        }

        public List<EC2InstanceModel> EC2Instances{ get; set; }
    }
}
