using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;

namespace StorageSample
{
    class Program
    {
        static void Main(string[] args)
        {
            dynamic result = DocumentAI.GetOperationStatusAsync("8787042348005361994").GetAwaiter().GetResult();
            dynamic configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            
            if(result.State == "SUCCEEDED"){
                foreach(var o in result.Files){
                    //  Console.WriteLine(o.InputPath);
                    var source = o.InputPath;
                    var p = $"gs://{configuration.integration.DocumentAI.gcs}/";
                    var text = StorageAPI.GetDocumentAIResultsAsync(o.OutputPath.Replace(p,"")).GetAwaiter().GetResult();
                    var metaData = StorageAPI.DownloadAsync(source.Replace("gs://",""), true).GetAwaiter().GetResult();
                    Console.WriteLine($"Source Content =>{metaData}");
                }

            }

            Console.Write(result.State);
            //Console.ReadLine();
        }
    }
}
