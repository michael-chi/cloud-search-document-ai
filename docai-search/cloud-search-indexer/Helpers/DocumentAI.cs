using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using cloud_search_fs.OAuth2;
using System.Linq;
//  * Reference: https://cloud.google.com/document-ai/docs/setup?hl=zh-tw#linux-or-macos
namespace cloud_search_fs
{
    //POST https://LOCATION-documentai.googleapis.com/v1beta3/projects/PROJECT_ID/locations/LOCATION/processors/PROCESSOR_ID:batchProcess
    /*
    {
        "inputConfigs": [
            {
            "gcsSource": "STORAGE_URI",
            "mimeType": "MIME_TYPE"
            }
        ],
        "outputConfig": {
            "gcsDestination": "OUTPUT_BUCKET"
        }
        }
    */
    public class DocumentAI
    {
        const string WAITING_FOLDER_NAME = "waiting";
        const string COMPLETED_FOLDER_NAME = "completed";
        internal static async Task EnsureStorageBuckets()
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            //  Ensure Bucket exists
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync($"https://storage.googleapis.com/storage/v1/b/{gcs}");
            var body = await resp.Content.ReadAsStringAsync();
            bool bucketExists = body.IndexOf("storage#bucket") > 0;
            if (!bucketExists)
            {
                resp = await client.PostAsync($"https://storage.googleapis.com/storage/v1/b?project={gcs_project}", new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        name = $"{gcs}"
                    }),
                Encoding.UTF8,
                "application/json"
                ));
            }

            //  Ensure Folder Exists
            await UploadAsync($"{WAITING_FOLDER_NAME}%2F", "");
            await UploadAsync($"{WAITING_FOLDER_NAME}%2F", "");

        }
        public static async Task<string> SendToStorageBucketAsync(string name, byte[] content)
        {
            return await UploadAsync($"{WAITING_FOLDER_NAME}/{name}", content);
        }
        public static async Task<string> SendToStorageBucketAsync(string name, string fullPath, byte[] content)
        {
            return await UploadAsync($"{WAITING_FOLDER_NAME}/{name}",$"file://{fullPath}", content);
        }
        private static async Task<string> UploadAsync(string name, byte[] content)
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            var client = await CreateHttpClientAsync();

            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{gcs}/o?name={name}";
            var resp = await client.PostAsync(url, new ByteArrayContent(
                content
            ));
            var result = await resp.Content.ReadAsStringAsync();
            Console.Write(result);

            return result;
        }
        private static async Task<string> UploadAsync(string name, string originalFilePath, byte[] content)
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            var client = await CreateHttpClientAsync();
            const string boundry_string = "part";
            var base64Content = Convert.ToBase64String(content);
            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{gcs}/o?name={name}";
            var mc = new MultipartContent(boundry_string);
            mc.Add(
                new StringContent(JsonConvert.SerializeObject(new { metadata = new { original_path = originalFilePath } }),
                Encoding.UTF8,
                "application/json"));
            mc.Add(
                new ByteArrayContent(
                content
            ));
            var resp = await client.PostAsync(url, mc);
            var result = await resp.Content.ReadAsStringAsync();
            Console.Write(result);

            return result;
        }
        private static async Task<string> UploadAsync(string name, string content)
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            var client = await CreateHttpClientAsync();

            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{gcs}/o?name={name}";
            var resp = await client.PostAsync(url, new StringContent(
                content,
                Encoding.UTF8,
                "application/json"
            ));
            var result = await resp.Content.ReadAsStringAsync();
            Console.Write(result);

            return result;
        }
        private static async Task<HttpClient> CreateHttpClientAsync()
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            HttpClient client = new HttpClient();
            await OAuth2HeaderHelper.UpdateDocAIOAuthHeaderAsync(client, $"{configuration.integration.DocumentAI.serviceAccountEmail}",
                                                    $"{configuration.integration.DocumentAI.keyFile}",
                                                    $"{configuration.integration.DocumentAI.password}");
            return client;
        }

        public static async Task<string> SmallFileOCRAsync(string file)
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var extension = file.Split('.').Last<string>();

            // HttpClient client = new HttpClient();
            // string sa = $"{configuration.integration.DocumentAI.serviceAccountEmail}";
            // string password = $"{configuration.integration.DocumentAI.password}";
            // await OAuth2HeaderHelper.UpdateDocAIOAuthHeaderAsync(client, sa, $"{configuration.integration.DocumentAI.keyFile}", password);
            var client = await CreateHttpClientAsync();
            string url = $"{configuration.integration.DocumentAI.small_ocr_url}";
            var fileContent = File.ReadAllBytes(file);
            var content = new StringContent(JsonConvert.SerializeObject(
                new
                {
                    document = new
                    {
                        mimeType = $"application/{extension}",
                        content = Convert.ToBase64String(fileContent)
                    }
                }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(url, content);
            var text = await response.Content.ReadAsStringAsync();
            File.WriteAllText($"/Users/kalschi/Documents/codes/cloud-search/cloud-search-fs/assets/{file.Split('/').Last()}.json",
                            text);
            return text;
        }
    }
}