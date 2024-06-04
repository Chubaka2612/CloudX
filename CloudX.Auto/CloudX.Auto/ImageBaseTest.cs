using System.IO;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CloudX.Auto.Tests
{
    public class ImageBaseTest : BaseTest
    {
        protected string ImageApiEndpoint = string.Empty;
        protected RestClient MyRestClient;

        protected int UploadFileViaApi(string filePath, string imageName, string fileNameToUpload)
        {
            var postRequest = new RestRequest(ImageApiEndpoint, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            postRequest.AddHeader("Content-Type", "multipart/form-data");
            postRequest.AddFile("upfile", () => File.OpenRead(Path.Combine(filePath, imageName)), fileNameToUpload);
            //obtain id of added image
            var postResponse = MyRestClient.Execute(postRequest);
            dynamic jsonResponse = JObject.Parse(postResponse.Content);

            return jsonResponse.id;
        }

        protected  bool FilesEqual(string filePath1, string filePath2)
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
