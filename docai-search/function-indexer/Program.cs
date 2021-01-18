using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;using Google.Events.Protobuf.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Framework.GcfEvents;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace StorageSample
{
    class Program
    {
        static void Main(string[] args)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            dynamic result = DocumentAI.GetOperationStatusAsync("8787042348005361994").GetAwaiter().GetResult();

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
                    int chunkSize = 4096;
                    var textContents = Enumerable.Range(0, sb.Length / chunkSize).Select(i => text.Substring(i * chunkSize, chunkSize))
                                .ToArray<string>();

                    var metaDataText = StorageAPI.DownloadAsync(source.Replace("gs://", ""), true).GetAwaiter().GetResult();
                    dynamic metaData = JObject.Parse(metaDataText);
                    var itemId = MD5Hash.Calculate($"{metaData.name}");
                    Console.WriteLine($"itemId={itemId}");
                    CloudSearchAPI.IndexSmallMediaFileAsync(itemId, 
                                                    $"{metaData.name}",
                                                    new string[] {$"{metaData.name}"},
                                                    $"{metaData.metadata.original_path}",
                                                    "TEXT",
                                                    DateTime.Parse($"{metaData.updated}"),
                                                    DateTime.Parse($"{metaData.timeCreated}"),
                                                    "TEXT",
                                                    DateTime.UtcNow.Ticks.ToString(),
                                                    textContents).GetAwaiter().GetResult();
                    Console.WriteLine(metaData);
                }

            }

            Console.Write(result.State);
        }
    }
}
