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

namespace CloudX.Auto.Tests.IAM.User
{
    public class IAMUserValidationTest : BaseTest
    {
        protected static IEnumerable<TestCaseData> RoleTestDataSource()
        {
            var sourceList = ConfigurationManager.Get<AWSEntriesModel>(nameof(AWSEntriesModel),
                "IAM\\User\\iam_user_test_data.json").AWSEntries;

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
        [TestCode("CXQA-IAM-04")]
        public async Task IAMUserShouldHaveCorrectGroupAssociated(AWSEntryModel expectedUserTestDataModel)
        {
            //obtain associated groups for a user with specific name
            var associatedGroups = await IAMService.Instance.ListGroupsForUser(expectedUserTestDataModel.Name);
            var attachedGroupsName = associatedGroups.Select(group => group.GroupName).ToList();

            AssertHelper.AreEquals(attachedGroupsName.Count, expectedUserTestDataModel.Values.Count,
                $"Verify amount of associate groups is correct for user with name '{expectedUserTestDataModel.Name}'");

            AssertHelper.CollectionEquals(attachedGroupsName, expectedUserTestDataModel.Values,
                $"Verify user with name {expectedUserTestDataModel.Name} has correct associated groups");
        }
    }
}
