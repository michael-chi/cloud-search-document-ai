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
        /*
            {
            "name": "projects/14568391036/locations/us/operations/8787042348005361994",
            "metadata": {
                "@type": "type.googleapis.com/google.cloud.documentai.v1beta3.BatchProcessMetadata",
                "state": "SUCCEEDED",
                "stateMessage": "Processed 1 document(s) successfully",
                "createTime": "2021-01-16T08:47:23.352247Z",
                "updateTime": "2021-01-16T08:50:23.508609Z",
                "individualProcessStatuses": [
                {
                    "inputGcsSource": "gs://kalschi-docai-2/waiting/02 office轉pdf檔案_中英文參雜敘述r1.pdf",
                    "outputGcsDestination": "gs://kalschi-docai-2/completed/8787042348005361994/0"
                }
                ]
            },
            "done": true,
            "response": {
                "@type": "type.googleapis.com/google.cloud.documentai.v1beta3.BatchProcessResponse"
            }
            }        
        */
        public static async Task<dynamic> GetOperationStatusAsync(string operationId){
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var url = $"{configuration.integration.DocumentAI.operation_url}{operationId}";
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
                Files = outputs.ToArray()
            };
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
    }
}