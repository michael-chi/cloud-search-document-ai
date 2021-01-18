using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StorageSample.OAuth2;

namespace StorageSample
{
    public class CloudSearchAPI
    {
        static public async Task<string> GetAsync(string itemId)
        {
            HttpClient client = await CreateHttpClientAsync();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}/{itemId}");

            return body;
        }
        static public async Task<string> ListAsync()
        {
            HttpClient client = await CreateHttpClientAsync();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}");

            return body;
        }

        private static async Task<HttpClient> CreateHttpClientAsync()
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            await OAuth2HeaderHelper.UpdateCloudSearchOAuthHeaderAsync(client, $"{configuration.integration.CloudSearch.serviceAccountEmail}",
                                                    $"{configuration.integration.CloudSearch.keyFile}",
                                                    $"{configuration.integration.CloudSearch.password}");
            return client;
        }
        static private string ConstructIndexPayload(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            object content, string version)
        {
            object itemContent = null;
            if (content is string)
            {
                itemContent = new
                {
                    inlineContent = System.Convert.ToBase64String(Encoding.UTF8.GetBytes((string)content)),
                    contentFormat = contentFormat
                };
            }
            else
            {
                itemContent = new
                {
                    contentDataRef = content,
                    contentFormat = contentFormat
                };
            }
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var base64Version = System.Convert.ToBase64String(Encoding.Default.GetBytes(version));
            var payload = new
            {
                item = new
                {
                    name = $"datasource/{configuration.integration.CloudSearch.datasource_id}/items/{itemId}",
                    acl = new
                    {
                        readers = new[]{
                                    new {
                                        gsuitePrincipal =  new {
                                            gsuiteDomain =  true
                                        }
                                        }
                                }
                    },
                    metadata = new
                    {
                        title = title,
                        sourceRepositoryUrl = url,
                        objectType = objectType,
                        createTime = createTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        updateTime = updateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        keywords = keywords
                    },
                    content = itemContent,
                    version = base64Version,
                    itemType = "CONTENT_ITEM"
                },
                mode = "SYNCHRONOUS",

            };

            return JsonConvert.SerializeObject(payload);
        }
        static public async Task IndexAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            string inLineContent, string version)
        {
            HttpClient client = await CreateHttpClientAsync();
            var bsae64Content = System.Convert.ToBase64String(Encoding.Default.GetBytes(inLineContent));
            var base64Version = System.Convert.ToBase64String(Encoding.Default.GetBytes(version));
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var c = ConstructIndexPayload(itemId, title, keywords, url, objectType, updateTime, createTime, contentFormat, inLineContent, version);
            
            var content = new StringContent(c,
                                            Encoding.UTF8,
                                            "application/json");
            HttpResponseMessage response = await client.PostAsync($"{configuration.integration.CloudSearch.url}/{itemId}:index", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.Write($"Index:{result}");
        }
        static public async Task IndexMediaFileAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                    DateTime updateTime, DateTime createTime, string contentFormat,
                                    string version, string[] contents)
        {
            dynamic itemRef = await StartUploadSessionAsync(itemId);

            foreach (var content in contents)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                Console.WriteLine($"itemRef.name={(string)itemRef["name"]}");
                await UploadMediaAsync(itemId, (string)itemRef["name"], bytes);

            }
            await IndexMediaAsync(itemId, title, keywords, url, objectType,
                                    updateTime, createTime, contentFormat, itemRef, version);
        }
        //  Search API limit: https://developers.google.com/cloud-search/docs/reference/limits
        static public async Task IndexSmallMediaFileAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            string version, string[] contents)
        {
            dynamic itemRef = await StartUploadSessionAsync(itemId);

            var content = contents[0].Substring(0, System.Math.Min(contents[0].Length, 1024 * 1000));
            Console.WriteLine($"itemRef.name={(string)itemRef["name"]}");
            await IndexAsync(itemId, title, keywords, url, objectType,
                                        updateTime, createTime, contentFormat,
                                        content, version);
        }
        /*
        Cloud Search indexes only the first 10 MB of a document if document exceed this size.
        */
        static private async Task IndexMediaAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            object itemRef, string version)
        {
            HttpClient client = await CreateHttpClientAsync();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var body = ConstructIndexPayload(itemId, title, keywords, url, objectType, updateTime, createTime, contentFormat,
                        itemRef, version);
            var content = new StringContent(body,
                                            Encoding.UTF8,
                                            "application/json");
            HttpResponseMessage response = await client.PostAsync($"{configuration.integration.CloudSearch.url}/{itemId}:index", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.Write($"Index:{result}");

            response.EnsureSuccessStatusCode();
        }
        private static async Task UploadMediaAsync(string itemId, string resourceName, byte[] content)
        {
            HttpClient client = await CreateHttpClientAsync();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            //  Step 1, Create an upload session
            var uploadUrl = $"https://cloudsearch.googleapis.com/upload/v1/media/{resourceName}";
            Console.WriteLine($"uploading...{uploadUrl}");
            using (var ms = new System.IO.MemoryStream(content))
            {
                try
                {
                    Console.WriteLine($"upload started at {DateTime.Now}");
                    var response = await client.PostAsync(uploadUrl, new StreamContent(ms));
                    var respBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"uploading:{respBody}");
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"upload error out at {DateTime.Now}");
                    Console.WriteLine($"{ex.Message}");
                    throw ex;

                }
            }
        }

        private static async Task<JObject> StartUploadSessionAsync(string itemId)
        {
            HttpClient client = await CreateHttpClientAsync();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            //var resp = c
            //  Step 1, Create an upload session
            var uploadUrl = $"https://cloudsearch.googleapis.com/v1/indexing/datasources/{configuration.integration.CloudSearch.datasource_id}/items/{itemId}:upload";
            Console.WriteLine($"indexing:{uploadUrl}");
            var body = JsonConvert.SerializeObject(new
            {
                connectorName = $"datasources/{configuration.integration.CloudSearch.datasource_id}/connectors/kalschidemoconnector"
            });
            var resp = await client.PostAsync(uploadUrl, new StringContent(body, Encoding.UTF8, "application/json"));
            /*
            {
            "name": "YWQyZWJkMjU2NWFjM2NkY2YyMTFiNTcwMjYxYTJiMzQvYnVja2V0LWFkMmViZDI1NjVhYzNjZGNmMjExYjU3MDI2MWEyYjM0LTUxMDU4OTkvYWQyZWJkMjU2NWFjM2NkY2YyMTFiNTcwMjYxYTJiMzQvNWZmZjkzN2QtMDAwMC0yNWUyLTk0NWQtMjQwNTg4NmU5NTU4"
            }
            */
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }
    }
}