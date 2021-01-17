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

        private static async Task<HttpClient> CreateHttpClientAsync()
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            HttpClient client = new HttpClient();
            await OAuth2HeaderHelper.UpdateDocAIOAuthHeaderAsync(client, $"{configuration.integration.DocumentAI.serviceAccountEmail}",
                                                    $"{configuration.integration.DocumentAI.keyFile}",
                                                    $"{configuration.integration.DocumentAI.password}");
            return client;
        }
        public static async Task<string> LargeFormParserAsync(string inputGcs,string contentType = "application/pdf")
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var url = $"{configuration.integration.DocumentAI.large_formParser_url}";
            var client = await CreateHttpClientAsync();
            var json = JsonConvert.SerializeObject(
                                    new
                                    {
                                        inputConfigs = new[]
                                            {
                                                new {
                                                    gcsSource= inputGcs,
                                                    mimeType= contentType
                                                    }
                                        },
                                        outputConfig = new
                                        {
                                            gcsDestination = $"gs://{configuration.integration.DocumentAI.gcs}/completed"
                                        }
                                    }
                            );

            Console.WriteLine(json);
            var resp = await client.PostAsync(url,new StringContent(json,
                                Encoding.UTF8,
                                "application/json")
                            );
            var ret = await resp.Content.ReadAsStringAsync();

            Console.WriteLine(ret);
            resp.EnsureSuccessStatusCode();
            return ret;
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