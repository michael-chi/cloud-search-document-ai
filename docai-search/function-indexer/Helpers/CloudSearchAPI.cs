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
            HttpClient client = new HttpClient();
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}/{itemId}");

            return body;
        }
        static public async Task<string> ListAsync()
        {
            HttpClient client = new HttpClient();
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}");

            return body;
        }
        static private async Task UpdateCloudSearchOAuthHeaderAsync(HttpClient client)
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            string sa = $"{configuration.integration.CloudSearch.serviceAccountEmail}";
            string password = $"{configuration.integration.CloudSearch.password}";
            await OAuth2HeaderHelper.UpdateCloudSearchOAuthHeaderAsync(client, sa, $"{configuration.integration.CloudSearch.keyFile}", password);
        }
                private static async Task<HttpClient> CreateHttpClientAsync()
        {
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            HttpClient client = new HttpClient();
            await OAuth2HeaderHelper.UpdateDocAIOAuthHeaderAsync(client, $"{configuration.integration.CloudSearch.serviceAccountEmail}",
                                                    $"{configuration.integration.CloudSearch.keyFile}",
                                                    $"{configuration.integration.CloudSearch.password}");
            return client;
        }
        static public async Task IndexAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            string inLineContent, string version)
        {
            HttpClient client = new HttpClient();
            var bsae64Content = System.Convert.ToBase64String(Encoding.Default.GetBytes(inLineContent));
            var base64Version = System.Convert.ToBase64String(Encoding.Default.GetBytes(version));
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var content = new StringContent(JsonConvert.SerializeObject(
                new
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
                            createTime = createTime,
                            updateTime = updateTime,
                            keywords = keywords
                        },
                        content = new
                        {
                            inlineContent = bsae64Content,
                            contentFormat = contentFormat
                        },
                        version = base64Version,
                        itemType = "CONTENT_ITEM"
                    },
                    mode = "SYNCHRONOUS",

                }),
                                            Encoding.UTF8,
                                            "application/json");
            HttpResponseMessage response = await client.PostAsync($"{configuration.integration.CloudSearch.url}/{itemId}:index", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.Write($"Index:{result}");
        }

        //  https://developers.google.com/cloud-search/docs/reference/rest/v1/media/upload#body.Media
        static public async Task IndexLargeFileAsync(string itemId, string title, string[] keywords, string url, string objectType,
                                            DateTime updateTime, DateTime createTime, string contentFormat,
                                            byte [] content, string version)
        {
            HttpClient client = new HttpClient();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var resp = c
            //  Step 1, Create an upload session
            var url = $"https://cloudsearch.googleapis.com/v1/indexing/datasources/{configuration.integration.CloudSearch.datasource_id}items/{itemId}:upload";

            var bsae64Content = System.Convert.ToBase64String(Encoding.Default.GetBytes(inLineContent));
            var base64Version = System.Convert.ToBase64String(Encoding.Default.GetBytes(version));
            await UpdateCloudSearchOAuthHeaderAsync(client);
            var content = new StringContent(JsonConvert.SerializeObject(
                new
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
                            createTime = createTime,
                            updateTime = updateTime,
                            keywords = keywords
                        },
                        content = new
                        {
                            inlineContent = bsae64Content,
                            contentFormat = contentFormat
                        },
                        version = base64Version,
                        itemType = "CONTENT_ITEM"
                    },
                    mode = "SYNCHRONOUS",

                }),
                                            Encoding.UTF8,
                                            "application/json");
            HttpResponseMessage response = await client.PostAsync($"{configuration.integration.CloudSearch.url}/{itemId}:index", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.Write($"Index:{result}");
        }
    }
}