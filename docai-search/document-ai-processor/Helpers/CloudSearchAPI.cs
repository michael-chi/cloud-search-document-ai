using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using cloud_search_fs.OAuth2;

/*
Get Started:
* https://developers.google.com/cloud-search/docs/tutorials/end-to-end/

Full Document:
* https://developers.google.com/cloud-search/docs/guides/content-connector#rest

API References:
* https://developers.google.com/cloud-search/docs/reference/rest/v1/indexing.datasources.items

Set up Search Application
* https://developers.google.com/cloud-search/docs/tutorials/end-to-end/setup-app#top_of_page
*/
namespace cloud_search_fs
{
    public class CloudSearchAPI
    {
        static public async Task<string> GetAsync(string itemId)
        {
            HttpClient client = new HttpClient();
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = ConfigHelper.ReadAppSettings();
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}/{itemId}");

            return body;
        }
        static public async Task<string> SearchAsync(string searchTerm)
        {
            var url = "https://cloudsearch.googleapis.com/v1/query/search";
            HttpClient client = new HttpClient();
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var body = new { 
                    query = searchTerm,
                    requestOptions = new {
                        languageCode = "en-US",
                        searchApplicationId = "searchapplications/default"
                    }
                };

            var resp = await client.PostAsync(url, new StringContent(
                                                    JsonConvert.SerializeObject(body),
                                                    Encoding.UTF8,
                                                    "application/json"
                                                ));
            var result = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Search Result\r\n{result}");
            resp.EnsureSuccessStatusCode();

            return result;
        }
        static public async Task<string> ListAsync()
        {
            HttpClient client = new HttpClient();
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = ConfigHelper.ReadAppSettings();
            string body = await client.GetStringAsync($"{configuration.integration.CloudSearch.url}");

            return body;
        }
        static private async Task UpdateCloudSearchOAuthHeaderAsync(HttpClient client)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            string sa = $"{configuration.integration.CloudSearch.serviceAccountEmail}";
            string password = $"{configuration.integration.CloudSearch.password}";
            await OAuth2HeaderHelper.UpdateCloudSearchOAuthHeaderAsync(client, sa, $"{configuration.integration.CloudSearch.keyFile}", password);
        }
        static public async Task IndexAsync(string itemId, string title, string[] keywords, string url, string objectType,
        DateTime updateTime, DateTime createTime, string contentFormat,
                                            string inLineContent, string version)
        {
            HttpClient client = new HttpClient();
            var bsae64Content = System.Convert.ToBase64String(Encoding.Default.GetBytes(inLineContent));
            var base64Version = System.Convert.ToBase64String(Encoding.Default.GetBytes(version));
            await UpdateCloudSearchOAuthHeaderAsync(client);
            dynamic configuration = ConfigHelper.ReadAppSettings();
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