using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudX.Auto.AWS.Core.Domain.S3;
using CloudX.Auto.Core.Attributes;
using CloudX.Auto.Core.Configuration;
using CloudX.Auto.Core.Meta;
using CloudX.Auto.Core.Utils;
using CloudX.Auto.Tests.Dto;
using CloudX.Auto.Tests.Models.TestData;
using CloudX.Auto.Tests.Steps.S3;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace CloudX.Auto.Tests.S3.Functional
{
    public class S3FunctionalValidationTest : ImageBaseTest
    {
        private const string s3TestDataFilePath = "S3\\s3_test_data.json";

        protected static S3BucketModel sourceBucket = ConfigurationManager.Get<S3BucketsModel>(nameof(S3BucketsModel),
               s3TestDataFilePath).S3Buckets.First();

        protected string bucketId = $"cloudximage-imagestorebucket{sourceBucket.Id}";
        protected string buckePrefix = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BuckePrefix"];
        protected string publicIp = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["PublicIP"];

        [SetUp]
        protected void BeforeEach()
        {
            ImageApiEndpoint = ConfigurationManager.GetConfiguration(s3TestDataFilePath)["BaseApiEndpoint"];
            Log.Debug("Initialize Rest client");
            MyRestClient = new RestClient($"http://{publicIp}");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-05")]
        [Order(1)]
        public async Task S3ViewListOfUploadedImages()
        {
            //s3 action
            var s3BucketObjectsList = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);
          
            //api action
            var getRequest = new RestRequest(ImageApiEndpoint, Method.Get);
            var getResponse = MyRestClient.Execute(getRequest);
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
                    DateTimeOffset.FromUnixTimeMilliseconds((long)imageObjectsDto.First(imageDto => imageDto.ObjectKey == s3ObjectKey).LastModified),
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
            var postRequest = new RestRequest(ImageApiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), fileNameToUpload);
            var postResponse = MyRestClient.Execute(postRequest);

            //s3 action
            var s3BucketObjectsListAfterUpload = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            AssertHelper.AreEquals(s3BucketObjectsListAfterUpload.Count, s3BucketObjectsListBeforeUpload.Count + 1,
                $"Veriy POST {postResponse.ResponseUri} added the resourse and size of s3 objects list was increased");
            AssertHelper.IsTrue(s3BucketObjectsListAfterUpload.Any(s3Object => s3Object.Key.Contains(fileNameToUpload)),
                $"Verify file with a proper name: '{fileNameToUpload}' was uploaded");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-03")]
        public async Task S3UploadImageWithTheSameNameToS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var filePath = "S3\\Resources";

            //s3 action
            var s3BucketObjectsListBeforeUpload = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            //api action
            var postRequest = new RestRequest(ImageApiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), imageName);
            var postResponse1 = MyRestClient.Execute(postRequest);//1st POST
            var postResponse2 = MyRestClient.Execute(postRequest);//2nd POST with the same parameters
            //s3 action
            var s3BucketObjectsListAfterUpload = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            AssertHelper.AreEquals(s3BucketObjectsListAfterUpload.Count, s3BucketObjectsListBeforeUpload.Count + 2,
                $"Veriy POST {postResponse1.ResponseUri} added the resourse and size of s3 objects list was increased");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-06")]
        public async Task S3DeleteImageFromS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var filePath = "S3\\Resources";

            //add test image to be deleted
            //api action
            var imageIdToDelete = UploadFileViaApi<int>(filePath, imageName, fileNameToUpload);

            //s3 action
            var s3BucketObjectsListBeforeDeletion = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            //retrieve full key of object to be deleted
            var imageKey = s3BucketObjectsListBeforeDeletion.First(s3Object => s3Object.Key.Contains(fileNameToUpload));

            //api action
            var deleteRequest = new RestRequest($"{ImageApiEndpoint}/{imageIdToDelete}", Method.Delete);
            var deleteResponse = MyRestClient.Execute(deleteRequest);

            //s3 action
            var s3BucketObjectsListAfterDeletion = await S3Service.Instance.ListS3ObjectsByKey(bucketId, buckePrefix);

            AssertHelper.AreEquals(s3BucketObjectsListAfterDeletion.Count, s3BucketObjectsListBeforeDeletion.Count - 1,
              $"Veriy DELETE {deleteResponse.ResponseUri} removed the resourse and size of s3 objects list was decreased");
            AssertHelper.IsFalse(s3BucketObjectsListAfterDeletion.Any(s3Object => s3Object.Key.Contains(fileNameToUpload)),
              $"Verify file with a proper name: '{fileNameToUpload}' was deleted");
        }

        [Test]
        [Component(ComponentName.CloudX_S3)]
        [Category(TestType.Regression)]
        [TestCode("CXQA-S3-04")]
        public async Task S3DownloadImageFromS3Bucket()
        {
            var imageName = "s3_test_photo.jpg";
            var fileNameToUpload = imageName.Split('.').First() + "_" + RandomStringUtils.RandomString(6) + ".jpg";
            var fileNameDownloaded = imageName.Split('.').First() + "_downloaded_" + RandomStringUtils.RandomString(6) + ".jpg";
            var sourcesFilePath = "S3/Resources";
            var downloadedFilePath = Path.Combine(sourcesFilePath, "Downloads");

            //add test image to be downloaded
            //api action
            var imageIdToDownload = UploadFileViaApi<int>(sourcesFilePath, imageName, fileNameToUpload);

            //api action
            var getRequest = new RestRequest($"{ImageApiEndpoint}/file/{imageIdToDownload}", Method.Get);
            var getResponse = MyRestClient.Execute(getRequest);

            File.Create(Path.Combine(downloadedFilePath, fileNameDownloaded)).Close();
            File.WriteAllBytes(Path.Combine(downloadedFilePath, fileNameDownloaded), getResponse.RawBytes);

            AssertHelper.IsTrue(FilesEqual(Path.Combine(sourcesFilePath, imageName), Path.Combine(downloadedFilePath, fileNameDownloaded)),
              $"Verify downloaded file as expected");

        }
    }
}
