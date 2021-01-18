using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using cloud_search_fs.OAuth2;
using System.Linq;

namespace cloud_search_fs
{

    public class StorageAPI
    {
        const string WAITING_FOLDER_NAME = "waiting";
        const string COMPLETED_FOLDER_NAME = "completed";
        internal static async Task EnsureStorageBuckets()
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
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
            await UploadAsync($"{COMPLETED_FOLDER_NAME}%2F", "");

        }
        public static async Task<string> SendToStorageBucketAsync(string name, byte[] content)
        {
            var text = await UploadAsync($"{WAITING_FOLDER_NAME}/{name}", content);

            dynamic o = JObject.Parse(text);
            string id = $"{o.id}";
            var generation = id.Split('/', StringSplitOptions.RemoveEmptyEntries).Last<string>();
            var result = $"gs://{id.Replace("/" + generation, "")}";
            return result;
        }

        public static async Task<string> SendToStorageBucketAsync(string name, string fullPath, byte[] content)
        {
            var text = await UploadAsync($"{WAITING_FOLDER_NAME}/{name}", $"http://{fullPath}", content);
            dynamic o = JObject.Parse(text);
            string id = $"{o.id}";
            var generation = id.Split('/', StringSplitOptions.RemoveEmptyEntries).Last<string>();
            var result = $"gs://{id.Replace("/" + generation, "")}";
            return result;
        }
        private static async Task<string> UploadAsync(string name, byte[] content)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            var client = await CreateHttpClientAsync();

            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{gcs}/o?name={name}";
            var resp = await client.PostAsync(url, new ByteArrayContent(
                content
            ));
            var result = await resp.Content.ReadAsStringAsync();
            Console.Write(result);
            
            resp.EnsureSuccessStatusCode();

            return result;
        }
        private static async Task<string> UploadAsync(string name, string originalFilePath, byte[] content)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
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
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadAsStringAsync();

            return result;
        }
        private static async Task<string> UploadAsync(string name, string content)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
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

            return result;
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