using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StorageSample.OAuth2;
using System.Linq;

namespace StorageSample
{
    public class DocumentAI
    {
        public static async Task<dynamic> GetOperationStatusAsync(string operationId){
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var url = $"{configuration.integration.DocumentAI.operation_url}{operationId}";
            Console.WriteLine($"operation url: {url}");
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var ret = await resp.Content.ReadAsStringAsync();
            dynamic json = JObject.Parse(ret);
            string state = $"{json.metadata.state}";
            //string outputGcs = $"{json.metadata.individualProcessStatuses.outputGcsDestination}";
            var outputs = from destination in (JArray)json.metadata.individualProcessStatuses
                            select new {
                                    OutputPath = $"{destination["outputGcsDestination"]}", 
                                    InputPath = $"{destination["inputGcsSource"]}" 
                                };
            var fileGroup = from output in outputs
                            group output by output.InputPath into g
                            select new {
                                InputFile = g.Key,
                                Output = g.ToArray().Select( o => o.OutputPath).ToArray()
                            };
            Console.WriteLine("===============");
            Console.WriteLine($"{JsonConvert.SerializeObject(fileGroup)}");
                            
            return new {
                State = state,
                Files = fileGroup.ToArray()
            };
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