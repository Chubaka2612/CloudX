using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.IAM.Dto;
using CloudX.Auto.AWS.Core.Domain.S3;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Steps.S3;
using CloudX.Auto.Tests.TestData.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.S3.Deployment
{
    public class S3FunctionalValidationTest : BaseTest
    {
        private const string s3TestDataFilePath = "S3\\s3_test_data.json";

        protected static S3BucketModel sourceBucket = ConfigurationManager.Get<S3BucketsModel>(nameof(S3BucketsModel),
               s3TestDataFilePath).S3Buckets.First();

        protected string bucketId = $"cloudximage-imagestorebucket{sourceBucket.Id}";
        protected string buckePrefix = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BuckePrefix"];
        protected string publicIp = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PublicIP"];
        protected string apiEndpoint = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BaseApiEndpoint"];
        protected RestClient client;

         [SetUp]
        protected void BeforeEach()
        {
            Log.Debug("Initialize Rest client");
            client = new RestClient($"http://{publicIp}");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-03")]
        [Order(1)]
        public async Task S3ViewListOfUploadedImages()
        {
            //s3 action
            var s3BucketObjectsList = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);
          
            //api action
            var getRequest = new RestRequest(apiEndpoint, Method.Get);
            var getResponse = client.Execute(getRequest);
            var imageObjectsDto = JsonConvert.DeserializeObject<List<ImageDto>>(getResponse.Content);
            var actualS3ObjectsKeys = s3BucketObjectsList.Select(s3Object => s3Object.Key).ToList();

            AssertHelper.AreEquals(s3BucketObjectsList.Count, imageObjectsDto.Select(imageDto => imageDto.Id).Count(),
                $"Veriy GET {getResponse.ResponseUri} returns expected amount of objects");
            AssertHelper.CollectionEquals(actualS3ObjectsKeys, imageObjectsDto.Select(imageObjectDto => imageObjectDto.ObjectKey).ToList(),
               "Verify image objects have expected 'Key'");

            foreach (var s3ObjectKey in actualS3ObjectsKeys) 
            {
                AssertHelper.AreEquals(S3Service.Instance.ListS3ObjectsMetaDataByKey(bucketId, s3ObjectKey).Result.Headers.ContentType,
                    imageObjectsDto.First(imageDto => imageDto.ObjectKey == s3ObjectKey).ObjectType, 
                    $"Verify 'ObjectType' is correct for s3 object by id: {s3ObjectKey}");

                AssertHelper.AreEquals(S3Service.Instance.ListS3ObjectsMetaDataByKey(bucketId, s3ObjectKey).Result.Headers.ContentLength,
                   imageObjectsDto.First(imageDto => imageDto.ObjectKey == s3ObjectKey).ObjectSize,
                   $"Verify 'ObjectSize' is correct for s3 object by id: {s3ObjectKey}");

                AssertHelper.AreEquals(S3Service.Instance.ListS3ObjectsMetaDataByKey(bucketId, s3ObjectKey).Result.LastModified,
                   imageObjectsDto.First(imageDto => imageDto.ObjectKey == s3ObjectKey).LastModified,
                   $"Verify 'LastModified' is correct for s3 object by id: {s3ObjectKey}");
            }

        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-03")]
        public async Task S3UploadImageToS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "S3\\Resources";

            //s3 action
            var s3BucketObjectsListBeforeUpload = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            //api action
            var postRequest = new RestRequest(apiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), fileNameToUpload);
            var postResponse = client.Execute(postRequest);

            //s3 action
            var s3BucketObjectsListAfterUpload = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            AssertHelper.AreEquals(s3BucketObjectsListBeforeUpload.Count + 1 , s3BucketObjectsListAfterUpload.Count,
                $"Veriy POST {postResponse.ResponseUri} added the resourse and size of s3 objects list was increased");
            AssertHelper.IsTrue(s3BucketObjectsListAfterUpload.Any(s3Object => s3Object.Key.Contains(fileNameToUpload)),
                $"Verify file with a proper name: '{fileNameToUpload}' was uploaded");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-03")]
        public async Task S3DeleteImageFromS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "S3\\Resources";

            //add test image to be deleted
            //api action
            string imageIdToDelete = UploadFileViaApi(filePath, imageName, fileNameToUpload);

            //s3 action
            var s3BucketObjectsListBeforeDeletion = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            //retrieve full key of object to be deleted
            var imageKey = s3BucketObjectsListBeforeDeletion.First(s3Object => s3Object.Key.Contains(fileNameToUpload));

            //api action
            var deleteRequest = new RestRequest($"{apiEndpoint}/{imageIdToDelete}", Method.Delete);
            var deleteResponse = client.Execute(deleteRequest);

            //s3 action
            var s3BucketObjectsListAfterDeletion = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            AssertHelper.AreEquals(s3BucketObjectsListBeforeDeletion.Count - 1, s3BucketObjectsListAfterDeletion.Count,
              $"Veriy DELETE {deleteResponse.ResponseUri} removed the resourse and size of s3 objects list was decreased");
            AssertHelper.IsFalse(s3BucketObjectsListAfterDeletion.Any(s3Object => s3Object.Key.Contains(fileNameToUpload)),
              $"Verify file with a proper name: '{fileNameToUpload}' was deleted");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-03")]
        public async Task S3DownloadImageFromS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var fileNameDownloaded = imageName.Split('.').First() + "_downloaded_" + RandomStringUtils.RandomString(6) + ".jpg";
            var sorcesFilePath = "S3/Resources";
            var downloadedFilePath = Path.Combine(sorcesFilePath, "Downloads");

            //add test image to be downloaded
            //api action
            string imageIdToDownload = UploadFileViaApi(sorcesFilePath, imageName, fileNameToUpload);

            //api action
            var getRequest = new RestRequest($"{apiEndpoint}/file/{imageIdToDownload}", Method.Get);
            var getResponse = client.Execute(getRequest);

            File.Create(Path.Combine(downloadedFilePath, fileNameDownloaded)).Close();
            File.WriteAllBytes(Path.Combine(downloadedFilePath, fileNameDownloaded), getResponse.RawBytes);

            AssertHelper.IsTrue(FilesEqual(Path.Combine(sorcesFilePath, imageName), Path.Combine(downloadedFilePath, fileNameDownloaded)),
              $"Veriy downloaded file as expected");

        }

        private string UploadFileViaApi(string filePath, string imageName, string fileNameToUpload)
        {
            var postRequest = new RestRequest(apiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), fileNameToUpload);
            //obtain id of added image
            var postResponse = client.Execute(postRequest);
            dynamic jsonResponse = JObject.Parse(postResponse.Content);

            return jsonResponse.id;
        }

        private static bool FilesEqual(string filePath1, string filePath2)
        {
            using (var fileStream1 = File.OpenRead(filePath1))
            using (var fileStream2 = File.OpenRead(filePath2))
            {
                if (fileStream1.Length != fileStream2.Length)
                {
                    return false;
                }

                int byte1;
                int byte2;

                do
                {
                    byte1 = fileStream1.ReadByte();
                    byte2 = fileStream2.ReadByte();

                    if (byte1 != byte2)
                    {
                        return false;
                    }
                } while (byte1 != -1 && byte2 != -1);

                return true;
            }
        }
    }
}
