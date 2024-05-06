using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using CloudX.Auto.Core.Configuration;

namespace CloudX.Auto.AWS.Core.Domain.IAM
{
    public class IAMService
    {
        private static IAMService _instance;

        private readonly IAmazonIdentityManagementService _iamService;
        
        private static readonly object Padlock = new object();

        private IAMService()
        {
            var accessKey = ConfigurationManager.IAMConfiguration.AccessKey;
            var secretKey = ConfigurationManager.IAMConfiguration.SecretKey;

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            _iamService = new AmazonIdentityManagementServiceClient(credentials);
        }

        public static IAMService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new IAMService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<List<ManagedPolicy>> ListPoliciesAsync()
        {
            var listPoliciesPaginator = _iamService.Paginators.ListPolicies(new ListPoliciesRequest());
            var policies = new List<ManagedPolicy>();

            await foreach (var response in listPoliciesPaginator.Responses)
            {
                policies.AddRange(response.Policies);
            }

            return policies;
        }

        public async Task<string> GetDefaultPolicyVersionAsync(string policyArn)
        {
            var defaultVersion = string.Empty;
            var responseVersions = await _iamService.ListPolicyVersionsAsync(
                new ListPolicyVersionsRequest
                {
                    PolicyArn = policyArn
                });

            responseVersions.Versions.ForEach(version =>
                {
                    if (version.IsDefaultVersion)
                    {
                        defaultVersion = version.VersionId;
                    }
                }
            );

            return defaultVersion;
        }

        public async Task<string> GetPolicyDocumentJsonAsync(string policyArn, string policyVersion)
        {
            var responsePolicy =
                await _iamService.GetPolicyVersionAsync(new GetPolicyVersionRequest
                {
                    PolicyArn = policyArn,
                    VersionId = policyVersion
                });
            return HttpUtility.UrlDecode(responsePolicy.PolicyVersion.Document);
        }

        public async Task<List<AttachedPolicyType>> ListAttachedRolePoliciesAsync(string roleName)
        {
            var attachedPolicies = new List<AttachedPolicyType>();
            var attachedRolePoliciesPaginator = _iamService.Paginators.ListAttachedRolePolicies(new ListAttachedRolePoliciesRequest { RoleName = roleName });

            await foreach (var response in attachedRolePoliciesPaginator.Responses)
            {
                attachedPolicies.AddRange(response.AttachedPolicies);
            }

            return attachedPolicies;
        }

        public async Task<List<AttachedPolicyType>> ListAttachedGroupPoliciesAsync(string groupName)
        {
            var attachedPolicies = new List<AttachedPolicyType>();
            var attachedGroupPoliciesPaginator = _iamService.Paginators.ListAttachedGroupPolicies(new ListAttachedGroupPoliciesRequest { GroupName = groupName });

            await foreach (var response in attachedGroupPoliciesPaginator.Responses)
            {
                attachedPolicies.AddRange(response.AttachedPolicies);
            }

            return attachedPolicies;
        }

        public async Task<List<Group>> ListGroupsForUser(string userName)
        {
            var groups = new List<Group>();
            var attachedUserGroupsPaginator = _iamService.Paginators.ListGroupsForUser(new ListGroupsForUserRequest { UserName = userName });

            await foreach (var response in attachedUserGroupsPaginator.Responses)
            {
                groups.AddRange(response.Groups);
            }

            return groups;
        }

        public async Task<GetInstanceProfileResponse> GetInstanceProfileAsync(string instanceProfileName)
        {
            var instanceProfileResponse = await _iamService.GetInstanceProfileAsync(new GetInstanceProfileRequest
            {
                InstanceProfileName = instanceProfileName
            });

            return instanceProfileResponse;

        }
    }
}
