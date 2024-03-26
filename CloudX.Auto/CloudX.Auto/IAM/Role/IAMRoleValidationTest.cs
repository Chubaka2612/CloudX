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

namespace CloudX.Auto.Tests.IAM.Role
{
    public class IAMRoleValidationTest : BaseTest
    {
        protected static IEnumerable<TestCaseData> RoleTestDataSource()
        {
            var sourceList = ConfigurationManager.Get<AWSEntriesModel>(nameof(AWSEntriesModel),
                "IAM\\Role\\iam_role_test_data.json").AWSEntries;

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
        [TestCode("CXQA-IAM-02")]
        public async Task IAMRoleShouldHaveCorrectAttachedPolicies(AWSEntryModel expectedRoleTestDataModel)
        {
            //obtain attached policies by required role
            var attachedPoliciesName = IAMService.Instance.ListAttachedRolePoliciesAsync(expectedRoleTestDataModel.Name)
                .Result.ToList().Select(policy => policy.PolicyName).ToList();

            AssertHelper.AreEquals(attachedPoliciesName.Count, expectedRoleTestDataModel.Values.Count,
                $"Verify amount of attached policies is correct for role with name '{expectedRoleTestDataModel.Name}'");

            AssertHelper.CollectionEquals(attachedPoliciesName, expectedRoleTestDataModel.Values, 
                $"Verify role with name {expectedRoleTestDataModel.Name} has correct attached policies");
        }
    }
}
