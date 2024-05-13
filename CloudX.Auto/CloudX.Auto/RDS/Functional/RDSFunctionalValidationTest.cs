using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Dto;
using CloudX.Auto.Tests.Models.RDS;
using CloudX.Auto.Tests.Models.TestData;
using CloudX.Auto.Tests.Steps.RDS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.RDS.Functional
{
    public class RDSFunctionalValidationTest : BaseTest
    {
        private const string RdsTestDataFilePath = "RDS\\rds_test_data.json";
        private const string S3TestDataFilePath = "S3\\s3_test_data.json";
      
        protected static MySqlConnectionModel MySqlConnectionModel = ConfigurationManager.Get<MySqlConnectionModel>(nameof(MySqlConnectionModel),
               RdsTestDataFilePath);

        protected string PublicIp = ConfigurationManager.GetConfiguration(RdsTestDataFilePath)["PublicIP"];
        protected string ApiEndpoint = ConfigurationManager.GetConfiguration(S3TestDataFilePath)["BaseApiEndpoint"];
        protected RestClient MyRestClient;
        protected MySqlClient MySqlClient;

        [SetUp]
        protected void BeforeEach()
        {
            Log.Debug("Initialize Rest client");
            MyRestClient = new RestClient($"http://{PublicIp}");

            Log.Debug("Initialize MySQL client");
            MySqlClient = new MySqlClient(MySqlConnectionModel);
            MySqlClient.Connect();//ssh tunneling
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-03")]
        public void RDSUploadedImageMetadataIsStoredInDatabase()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi(filePath, imageName, fileNameToUpload);
           
            //db action
            var images = MySqlClient.ExecuteReader(
                $"SELECT id, object_key, object_type, object_size, last_modified FROM images WHERE object_key LIKE '%{fileNameToUpload}%'",
                reader => new ImageEntity()
                {
                    Id = reader.GetInt32("id"),
                    ObjectKey = reader.GetString("object_key"),
                    ObjectType = reader.GetString("object_type"),
                    ObjectSize = reader.GetInt32("object_size"),
                    LastModified = reader.GetDateTime("last_modified"),
                }
            );

            //assert
            AssertHelper.AreEquals(images.Count, 1 ,"Verify only one image was uploaded to DB");
            AssertHelper.AssertScope(
                () => AssertHelper.IsTrue(images.First().ObjectKey.Contains(fileNameToUpload), "Verify uploaded image 'ObjectKey' is correct"),
                () => AssertHelper.AreEquals(images.First().ObjectType, "binary/octet-stream", "Verify uploaded image 'ObjectType' is correct"),
                () => AssertHelper.AreEquals(images.First().ObjectSize, new FileInfo(Path.Combine(filePath, imageName)).Length, 
                    "Verify uploaded image 'ObjectSize' is correct"),
                () => AssertHelper.DatesAreEqualWithOffset( DateTimeOffset.Now.ToUniversalTime(), images.First().LastModified,
                    message: "Verify uploaded image 'LastModified' time is correct")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-04")]
        public void RDSImageMetadataCanBeReturnedByApiCall()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi(filePath, imageName, fileNameToUpload);

            //db action
            var imageEntity = MySqlClient.ExecuteReader(
                $"SELECT id, object_key, object_type, object_size, last_modified FROM images WHERE id = '{imageId}'",
                reader => new ImageEntity()
                {
                    Id = reader.GetInt32("id"),
                    ObjectKey = reader.GetString("object_key"),
                    ObjectType = reader.GetString("object_type"),
                    ObjectSize = reader.GetInt32("object_size"),
                    LastModified = reader.GetDateTime("last_modified"),
                }
            ).FirstOrDefault();

            //api action
            var getRequest = new RestRequest($"{ApiEndpoint}/{imageId}");
            var getResponse = MyRestClient.Execute(getRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(getResponse.Content);

            //assert
            AssertHelper.IsNotNull(imageEntity, $"Verify image with id {imageId} is present in DB");
            AssertHelper.AssertScope(
                () => AssertHelper.AreEquals(imageEntity.ObjectKey, imageDto.ObjectKey, "Verify uploaded image 'ObjectKey' has the same value as ImageDto"),
                () => AssertHelper.AreEquals(imageEntity.ObjectType, imageDto.ObjectType, "Verify uploaded image 'ObjectType' has the same value as ImageDto"),
                () => AssertHelper.AreEquals(imageEntity.ObjectSize,  imageDto.ObjectSize,
                    "Verify uploaded image 'ObjectSize' has the same value as ImageDto"),
                () => AssertHelper.DatesAreEqualWithOffset(imageEntity.LastModified, imageDto.LastModified,
                    message: "Verify uploaded image 'LastModified' time  has the same value as ImageDto")
                );
        }

        [Test]
        [Component(ComponentName.CloudX_RDS)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-RDS-05")]
        public void RDSImageMetadataShouldBeDeletedFromDB()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "RDS\\Resources";

            //api action
            var imageId = UploadFileViaApi(filePath, imageName, fileNameToUpload);

            //api action
            var deleteRequest = new RestRequest($"{ApiEndpoint}/{imageId}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);
            AssertHelper.IsTrue(deleteResponse.Content.Contains( "Image is deleted"), "Verify image is deleted via API");

            //db action
            var imageEntity = MySqlClient.ExecuteReader(
                $"SELECT id, object_key, object_type, object_size, last_modified FROM images WHERE id = '{imageId}'",
                reader => new ImageEntity()
                {
                    Id = reader.GetInt32("id"),
                    ObjectKey = reader.GetString("object_key"),
                    ObjectType = reader.GetString("object_type"),
                    ObjectSize = reader.GetInt32("object_size"),
                    LastModified = reader.GetDateTime("last_modified"),
                }
            ).FirstOrDefault();
          
            //assert
            AssertHelper.IsNull(imageEntity, $"Verify image with id {imageId} was deleted from DB");
        }

        private int UploadFileViaApi(string filePath, string imageName, string fileNameToUpload)
        {
            var postRequest = new RestRequest(ApiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), fileNameToUpload);
            //obtain id of added image
            var postResponse = MyRestClient.Execute(postRequest);
            var imageDto = JsonConvert.DeserializeObject<ImageDto>(postResponse.Content);

            return imageDto.Id;
        }

        [TearDown]
        protected void AfterEach()
        {
            Log.Debug("Close MySQL client");
            MySqlClient.Disconnect();
        }
    }
}
