using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace CloudX.Auto.AWS.Core.Domain.SLESS
{
    public class DynamoDbService
    {
        private static DynamoDbService _instance;

        private readonly IAmazonDynamoDB _dynamoDbService;

        private static readonly object Padlock = new object();

        private DynamoDbService()
        {
            _dynamoDbService = new AmazonDynamoDBClient();
        }

        public IAmazonDynamoDB GetClient()
        {
            return _dynamoDbService;
        }

        public static DynamoDbService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DynamoDbService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<TableDescription> GetDynamoDbTableDescriptionAsync(string tableNamePrefix)
        {
            var listTablesResponse = await _dynamoDbService.ListTablesAsync();

            var matchingTableName = listTablesResponse
                                        .TableNames
                                        .FirstOrDefault(name => name.StartsWith(tableNamePrefix)) ?? 
                                    throw new Exception($"No tables found with prefix '{tableNamePrefix}'");
            var describeTableResponse = await _dynamoDbService.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = matchingTableName
            });

            return describeTableResponse.Table;
        }

        public async Task<List<Tag>> GetDynamoDbTableTagsAsync(string tableArn)
        {
            ListTagsOfResourceResponse listTagsResponse;
            try
            {
                 listTagsResponse = await _dynamoDbService.ListTagsOfResourceAsync(new ListTagsOfResourceRequest
                {
                    ResourceArn = tableArn
                });
            }
            catch
            {
                throw new Exception("Can't obtain SNS attributes");
            }
            return listTagsResponse.Tags;
        }

        public async Task<ScanResponse> ScanTable(string tableNamePrefix)
        {
            var table = await GetDynamoDbTableDescriptionAsync(tableNamePrefix);

            var scanRequest = new ScanRequest
            {
                TableName = table.TableName
            };

            return await _dynamoDbService.ScanAsync(scanRequest);
        }

    }
}