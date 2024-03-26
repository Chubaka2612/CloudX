using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.IAM;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.TestData.Model;
using NUnit.Framework;

namespace CloudX.Auto.Tests.IAM.Group
{
    public class IAMGroupValidationTest : BaseTest
    {
        protected static IEnumerable<TestCaseData> RoleTestDataSource()
        {
            var sourceList = ConfigurationManager.Get<AWSEntriesModel>(nameof(AWSEntriesModel),
                "IAM\\Group\\iam_group_test_data.json").AWSEntries;

            for (var i = 0; i < sourceList.Count; i++)
            {
                var tcd = new TestCaseData(sourceList.ElementAt(i));
                tcd.SetName("{m}[" + sourceList.ElementAt(i).Name + "]");
                yield return tcd;
            }
        }

        [Test]
        [Component(ComponentName.CloudX_IAM)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(RoleTestDataSource))]
        [TestCode("CXQA-IAM-03")]
        public async Task IAMGroupShouldHaveCorrectAttachedPolicies(AWSEntryModel expectedGroupTestDataModel)
        {
            //obtain attached policies by required group
            var attachedPolicies = await IAMService.Instance.ListAttachedGroupPoliciesAsync(expectedGroupTestDataModel.Name);
            var attachedPoliciesName = attachedPolicies.Select(policy => policy.PolicyName).ToList();

            AssertHelper.AreEquals(attachedPoliciesName.Count, expectedGroupTestDataModel.Values.Count,
                $"Verify amount of attached policies is correct for group with name '{expectedGroupTestDataModel.Name}'");

            AssertHelper.CollectionEquals(attachedPoliciesName, expectedGroupTestDataModel.Values, 
                $"Verify group with name {expectedGroupTestDataModel.Name} has correct attached policies");
        }
    }
}
