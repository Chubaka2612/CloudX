using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.SLESS;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Dto;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.SLESS.Functional
{
    //Reworked suite from RDS
    public class DynamoDbFunctionalValidationTest : ImageBaseTest
    {
        private const string SLESSTestDataFilePath = "SLESS\\sless_test_data.json";
        private string  tablePrefix;
        
        [SetUp]
        protected void BeforeEach()
        {
            var properties = ReadConfig(SLESSTestDataFilePath);
            tablePrefix = properties["DynamoDb"]["Name"].ToString();

            var publicIp = ConfigurationManager.GetConfiguration(SLESSTestDataFilePath)["PublicIP"];
            ImageApiEndpoint = ConfigurationManager.GetConfiguration(SLESSTestDataFilePath)["BaseImageApiEndpoint"];
            MyRestClient = new RestClient($"http://{publicIp}");
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-03")]
        public async Task DynamoDbUploadedImageMetadataIsStoredInDatabase()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);

            //db action
            var images = (await DynamoDbService.Instance.ScanTable(tablePrefix)).Items
                .Where(item => item["id"].S == imageId)
                .ToList();
            //assert
            AssertHelper.AreEquals(images.Count, 1, "Verify only one image was uploaded to DB");
            AssertHelper.AssertScope(
                () => AssertHelper.IsTrue(images.First()["object_key"].S.Contains(fileNameToUpload),
                    "Verify uploaded image 'ObjectKey' is correct"),
                () => AssertHelper.AreEquals(images.First()["object_type"].S, "binary/octet-stream",
                    "Verify uploaded image 'ObjectType' is correct"),
                () => AssertHelper.AreEquals(images.First()["object_size"].N,
                    new FileInfo(Path.Combine(filePath, imageName)).Length.ToString(),
                    "Verify uploaded image 'ObjectSize' is correct"),
                () => AssertHelper.NumbersAreEqualWithOffset(DateTimeOffset.Now.ToUnixTimeSeconds(),
                    double.Parse(images.First()["last_modified"].N),
                    message: "Verify uploaded image 'LastModified' time is correct")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-04")]
        public async Task DynamoDbImageMetadataCanBeReturnedByApiCall()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);

            //db action
            var image = (await DynamoDbService.Instance.ScanTable(tablePrefix)).Items
                .First(item => item["id"].S == imageId);

            //api action
            var getRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            //assert
            AssertHelper.IsNotNull(image, $"Verify image with id {imageId} is present in Dynamo DB");
            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(image["object_key"].S, imageDto.ObjectKey,
                    "Verify uploaded image 'ObjectKey' has the same value as ImageDto"),
                () => AssertHelper.AreEquals(image["object_type"].S, imageDto.ObjectType,
                    "Verify uploaded image 'ObjectType' has the same value as ImageDto"),
                () => AssertHelper.AreEquals(image["object_size"].N, imageDto.ObjectSize.ToString(),
                    "Verify uploaded image 'ObjectSize' has the same value as ImageDto"),
                () => AssertHelper.NumbersAreEqualWithOffset(double.Parse(image["last_modified"].N), imageDto.LastModified,
                     message: "Verify uploaded image 'LastModified' time  has the same value as ImageDto")
            );
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-05")]
        public async Task DynamoDbImageMetadataShouldBeDeletedFromDB()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi<string>(filePath, imageName, fileNameToUpload);

            //api action
            var deleteRequest = new RestRequest($"{ImageApiEndpoint}/{imageId}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);
            AssertHelper.IsTrue(deleteResponse.Content.Contains("Image is deleted"), "Verify image is deleted via API");

            //db action
            var image = (await DynamoDbService.Instance.ScanTable(tablePrefix)).Items
                .FirstOrDefault(item => item["id"].S == imageId);

            //assert
            AssertHelper.IsNull(image, $"Verify image with id {imageId} was deleted from DB");
        }
    }
}
