using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StorageSample.OAuth2;
using System.Linq;
using System.Collections.Generic;
namespace StorageSample
{

    public class StorageAPI
    {
        private static string ConstructMetadataLink(string objectId /* /{bucket_name}/{object_name} */, bool metadataOnly)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            Console.WriteLine(objectId);
            var bucket = objectId.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
            objectId = objectId.StartsWith("/") ?
                                        objectId.Replace("/" + bucket + "/", "") :
                                        objectId.Replace(bucket + "/", "");
            var encodedObjectId = Uri.EscapeDataString(objectId);
            var url = metadataOnly ?
                            $"https://storage.googleapis.com/storage/v1/b/{bucket}/o/{encodedObjectId}?alt=json" :
                            $"https://storage.googleapis.com/download/storage/v1/b/{bucket}/o/{encodedObjectId}?alt=media";

            return url;
        }
        
        public static async Task<string> GetDocumentAIResultsAsync(string folder)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var url = $"https://storage.googleapis.com/storage/v1/b/{configuration.integration.DocumentAI.gcs}/o?prefix={folder}";
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(url);

            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            dynamic json = JObject.Parse(body);

            var docAiResults = (from JToken item in (JArray)json.items
                                select new
                                {
                                    Id = (string)item["id"],
                                    Bucket = (string)item["bucket"],
                                    Generation = (string)item["generation"],
                                    FileName = ((string)item["name"]).Split('/').Last(),
                                    MediaLink = (string)item["mediaLink"],
                                    SelfLink = (string)item["selfLink"]
                                }).ToArray();

            List<string> ret = new List<string>();
            foreach (dynamic docAiResult in docAiResults)
            {
                string content = await DownloadDocumentAIOcrTextResultAsync(docAiResult.MediaLink);
                //Console.WriteLine(">>>>>" + content.Substring(0, 30));
                ret.Add(content);
            }
            return string.Join(Environment.NewLine, ret.ToArray());
        }
        public static async Task<string> DownloadAsync(string objectId, bool metadataOnly)
        {
            var mediaLink = ConstructMetadataLink(objectId, metadataOnly);

            Console.WriteLine($"MediaLink={mediaLink}");
            var result = await DownloadObjectAsync(mediaLink);

            return result;
        }
        private static async Task<string> DownloadObjectAsync(string mediaLink)
        {
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(mediaLink);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            return body;
        }
        public static async Task<string> DownloadDocumentAIOcrTextResultAsync(string mediaLink)
        {
            var body = await DownloadObjectAsync(mediaLink);
            var text = (string)JObject.Parse(body)["text"];
            return text;
        }

        private static async Task<HttpClient> CreateHttpClientAsync()
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            
            HttpClient client = new HttpClient();
            await OAuth2HeaderHelper.UpdateDocAIOAuthHeaderAsync(client, $"{configuration.integration.DocumentAI.serviceAccountEmail}",
                                                    $"{configuration.integration.DocumentAI.keyFile}",
                                                    $"{configuration.integration.DocumentAI.password}");
            return client;
        }

    }
}