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

            if (result.State == "SUCCEEDED")
            {
                foreach (var o in result.Files)
                {
                    var source = o.InputFile;
                    var sb = new System.Text.StringBuilder();
                    var p = $"gs://{configuration.integration.DocumentAI.gcs}/";
                    foreach (var outputPath in o.Output)
                    {
                        var text = StorageAPI.GetDocumentAIResultsAsync(outputPath.Replace(p, "")).GetAwaiter().GetResult();
                        sb.Append(text).Append(Environment.NewLine);
                    }
                    var metaDataText = StorageAPI.DownloadAsync(source.Replace("gs://", ""), true).GetAwaiter().GetResult();
                    dynamic metaData = JObject.Parse(metaDataText);
                    CloudSearchAPI.IndexAsync($"{metaData.name}", $"{metaData.name}",
                                              new string[] {$"{metaData.name}"}, $"{metaData.metadata.original_path}",
                                              "TEXT", DateTime.Parse($"{metaData.updated}"), DateTime.Parse($"{metaData.timeCreated}"), "TEXT",
                                              sb.ToString(), DateTime.UtcNow.Ticks.ToString()).GetAwaiter().GetResult();
                    Console.WriteLine(metaData);
                }
                
            }

            Console.Write(result.State);
            //Console.ReadLine();
        }
    }
}
