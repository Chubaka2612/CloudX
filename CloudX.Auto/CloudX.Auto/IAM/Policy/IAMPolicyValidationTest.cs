using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.IAM;
using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Models.TestData;
using NUnit.Framework;

namespace CloudX.Auto.Tests.IAM.Policy
{
    public class IAMPolicyValidationTest : BaseTest
    {
        protected static IEnumerable<TestCaseData> PolicyTestDataSource()
        {
            var sourceList = ConfigurationManager.Get<PoliciesModel>(nameof(PoliciesModel),
                "IAM\\Policy\\iam_policy_test_data.json").Policies;

            for (int i = 0; i < sourceList.Count; i++)
            {
                var tcd = new TestCaseData(sourceList.ElementAt(i));
                tcd.SetName("{m}[" + sourceList.ElementAt(i).Name + "]");
                yield return tcd;
            }
        }

        [Test]
        [Component(ComponentName.CloudX_IAM)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(PolicyTestDataSource))]
        [TestCode("CXQA-IAM-01")]
        public async Task IAMPlicyShouldHaveCorrectStatementLength(PolicyModel expectedPolicyTestDataModel)
        {
            //obtain policy with required name
            var policy = IAMService.Instance.ListPoliciesAsync().Result.ToList()
                             .FirstOrDefault(policy => policy.PolicyName == expectedPolicyTestDataModel.Name)
                         ?? throw new ArgumentException(
                             $"No policy with name '{expectedPolicyTestDataModel.Name}' where found in PolicyList");

            //obtain default version for a specific policy
            var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.Arn);

            //obtain policy doc and map it to dto
            var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.Arn, version);
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            AssertHelper.AreEquals(policyDocDto.Statement.Count, 1,
                $"Verify '{expectedPolicyTestDataModel.Name}' has correct 'Statement' length");
        }

        [Test]
        [Component(ComponentName.CloudX_IAM)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(PolicyTestDataSource))]
        [TestCode("CXQA-IAM-01")]
        public async Task IAMPlicyShouldHaveExpectedResource(PolicyModel expectedPolicyTestDataModel)
        {
            //obtain policy with required name
            var policy = IAMService.Instance.ListPoliciesAsync().Result.ToList()
                             .FirstOrDefault(policy => policy.PolicyName == expectedPolicyTestDataModel.Name)
                         ?? throw new ArgumentException(
                             $"No policy with name '{expectedPolicyTestDataModel.Name}' where found in PolicyList");

            //obtain default version for a specific policy
            var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.Arn);

            //obtain policy doc and map it to dto
            var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.Arn, version);
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            AssertHelper.AreEquals(policyDocDto.Statement.First().Resource.First(), expectedPolicyTestDataModel.Resource,
                $"Verify '{expectedPolicyTestDataModel.Name}' has correct 'Resource' value");
        }

        [Test]
        [Component(ComponentName.CloudX_IAM)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(PolicyTestDataSource))]
        [TestCode("CXQA-IAM-01")]
        public async Task IAMPlicyShouldHaveExpectedEffect(PolicyModel expectedPolicyTestDataModel)
        {
            //obtain policy with required name
            var policy = IAMService.Instance.ListPoliciesAsync().Result.ToList()
                             .FirstOrDefault(policy => policy.PolicyName == expectedPolicyTestDataModel.Name)
                         ?? throw new ArgumentException(
                             $"No policy with name '{expectedPolicyTestDataModel.Name}' where found in PolicyList");

            //obtain default version for a specific policy
            var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.Arn);

            //obtain policy doc and map it to dto
            var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.Arn, version);
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            AssertHelper.AreEquals(policyDocDto.Statement.First().Effect, expectedPolicyTestDataModel.Effect,
                $"Verify '{expectedPolicyTestDataModel.Name}' has correct 'Effect' value");
        }

        [Test]
        [Component(ComponentName.CloudX_IAM)]
        [Category(TestType.Regression)]
        [TestCaseSource(nameof(PolicyTestDataSource))]
        [TestCode("CXQA-IAM-01")]
        public async Task IAMPlicyShouldHaveExpectedAction(PolicyModel expectedPolicyTestDataModel)
        {
            //obtain policy with required name
            var policy = IAMService.Instance.ListPoliciesAsync().Result.ToList()
                             .FirstOrDefault(policy => policy.PolicyName == expectedPolicyTestDataModel.Name)
                         ?? throw new ArgumentException(
                             $"No policy with name '{expectedPolicyTestDataModel.Name}' where found in PolicyList");

            //obtain default version for a specific policy
            var version = await IAMService.Instance.GetDefaultPolicyVersionAsync(policy.Arn);

            //obtain policy doc and map it to dto
            var policyDocJson = await IAMService.Instance.GetPolicyDocumentJsonAsync(policy.Arn, version);
            var policyDocDto = CommonUtils.PopulateFromJson<PolicyDocumentDto>(policyDocJson);

            AssertHelper.CollectionEquals(policyDocDto.Statement.First().Action, expectedPolicyTestDataModel.Action,
                $"Verify '{expectedPolicyTestDataModel.Name}' has correct 'Action'");
        }

    }
}
