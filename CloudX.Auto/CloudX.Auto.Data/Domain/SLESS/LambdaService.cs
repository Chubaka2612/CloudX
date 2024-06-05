using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace CloudX.Auto.AWS.Core.Domain.SLESS
{
    public class LambdaService
    {
        private static LambdaService _instance;

        private readonly IAmazonLambda _lambdaService;

        private static readonly object Padlock = new object();

        private LambdaService()
        {
            _lambdaService = new AmazonLambdaClient();
        }

        public IAmazonLambda GetClient()
        {
            return _lambdaService;
        }

        public static LambdaService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LambdaService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<FunctionConfiguration> GetLambdaFunctionConfigurationAsync(string functionNamePrefix)
        {
            var request = new ListFunctionsRequest();
            var response = await _lambdaService.ListFunctionsAsync(request);

            var functionConfiguration = response.Functions
                .FirstOrDefault(func => func.FunctionName.StartsWith(functionNamePrefix, StringComparison.OrdinalIgnoreCase));

            return functionConfiguration;
        }

        public async Task<List<EventSourceMappingConfiguration>> GetLambdaEventSourceMappingsAsync()
        {
            var request = new ListEventSourceMappingsRequest();
            var response = await _lambdaService.ListEventSourceMappingsAsync(request);
            return response.EventSourceMappings;
        }

        public async Task<Dictionary<string, string>> GetLambdaFunctionTagsAsync(string resourceArn)
        {
            var response = await _lambdaService.ListTagsAsync(new ListTagsRequest
            {
                Resource = resourceArn
            });

            return response.Tags;
        }
    }
}