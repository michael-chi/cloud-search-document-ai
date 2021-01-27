using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Google.Events.Protobuf.Cloud.Storage.V1;
using CloudNative.CloudEvents;
// using Google.Cloud.Functions.Framework;
// using Google.Cloud.Functions.Framework.GcfEvents;
// using Microsoft.Extensions.Logging;

using System.Threading;
using System.Collections;

namespace StorageSample
{
    /*
        This is for local tests
    */
    class Program
    {
        static void Main(string[] args)
        {
            Test2();

        }
        static void Test2()
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            dynamic result = DocumentAI.GetOperationStatusAsync("13566904154644279726").GetAwaiter().GetResult();

            if (result.State == "SUCCEEDED")
            {
                List<KeyValuePair<string,string>> fields = new List<KeyValuePair<string, string>>();
                foreach (var o in result.Files)
                {
                    var source = o.InputFile;
                    var p = $"gs://{configuration.integration.DocumentAI.gcs}/";
                    foreach (var outputPath in o.Output)
                    {
                        var items = StorageAPI.GetFormFieldsResultsAsync(outputPath.Replace(p, "")).GetAwaiter().GetResult();
                        fields.AddRange(items);
                    }
                }
                var csv = StorageAPI.CreateCSV(fields.ToArray());
                System.IO.File.WriteAllText("csv.csv",csv);
                Console.WriteLine(JsonConvert.SerializeObject(fields.ToArray()));
                        
                var text = Newtonsoft.Json.JsonConvert.SerializeObject(fields.ToArray());
                System.IO.File.WriteAllText("./result.json", text);
            }
        }

        static void Test()
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            dynamic result = DocumentAI.GetOperationStatusAsync("13566904154644279726").GetAwaiter().GetResult();

            if (result.State == "SUCCEEDED")
            {
                foreach (var o in result.Files)
                {
                    var source = o.InputFile;
                    var sb = new System.Text.StringBuilder();
                    var p = $"gs://{configuration.integration.DocumentAI.gcs}/";
                    foreach (var outputPath in o.Output)
                    {
                        var ocrText = StorageAPI.GetDocumentAIResultsAsync(outputPath.Replace(p, "")).GetAwaiter().GetResult();
                        sb.Append(ocrText).Append(Environment.NewLine);
                    }
                    string text = sb.ToString();

                    var metaDataText = StorageAPI.DownloadAsync(source.Replace("gs://", ""), true).GetAwaiter().GetResult();
                    dynamic metaData = JObject.Parse(metaDataText);
                    var itemId = MD5Hash.Calculate($"{metaData.name}");

                    CloudSearchAPI.IndexSmallMediaFileAsync(itemId,
                                                    $"{metaData.name}",
                                                    new string[] { $"{metaData.name}" },
                                                    $"{metaData.metadata.original_path}",
                                                    "TEXT",
                                                    DateTime.Parse($"{metaData.updated}"),
                                                    DateTime.Parse($"{metaData.timeCreated}"),
                                                    "TEXT",
                                                    DateTime.UtcNow.Ticks.ToString(),
                                                    sb.ToString()).GetAwaiter().GetResult();
                }

            }
        }
    }
}
