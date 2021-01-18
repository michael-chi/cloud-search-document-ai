using Google.Events.Protobuf.Cloud.Storage.V1;
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
using Newtonsoft.Json.Linq;
using System.Linq;

namespace StorageSample
{
    public class Function : ICloudEventFunction<StorageObjectData>
    {
        public Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
        {
            Console.WriteLine($"HandleAsync()::data.Id = {data.Id}");
            //dynamic configuration = JObject.Parse(File.ReadAllText("./appsettings.json"));
            dynamic configuration = ConfigHelper.ReadAppSettings();
            if (cloudEvent.Type == "google.cloud.storage.object.v1.finalized" &&
                    data.Id.StartsWith($"{configuration.integration.DocumentAI.gcs}/completed/"))
            {
                //kalschi-docai-2/completed/1356538610275523562/0/01 office轉pdf檔案_純中文文字敘述r1.pdf/1610850507909609
                var gen = "/" + data.Generation;
                var url = $"gs://{data.Id.Replace(gen, "")}";
                var operationId = data.Id.Split('/')[2];
                Console.WriteLine($"checking Document AI result for {operationId}");
                dynamic result = DocumentAI.GetOperationStatusAsync(operationId).GetAwaiter().GetResult();
                Console.WriteLine($"Document AI result for {operationId} is {result.State}");

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
                                                        new string[] { $"{metaData.name}" },
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
            }
            // Console.WriteLine($"CloudEvent type: {JsonConvert.SerializeObject(cloudEvent)}");
            // Console.WriteLine($"Storage bucket: {data}");
            // Console.WriteLine($"Storage object name: {data.Name}");
            return Task.CompletedTask;
        }
    }
}
