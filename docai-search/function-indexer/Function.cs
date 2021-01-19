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
        /*
        Cloud Functions entry point, this is where we get Storage events and respond to it.
        In this case, we will collect all Document AI result files (they are placed in the Storage Bucket we specified)
        */
        public Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            if (cloudEvent.Type == "google.cloud.storage.object.v1.finalized" &&
                    data.Id.StartsWith($"{configuration.integration.DocumentAI.gcs}/completed/"))
            {
                var gen = "/" + data.Generation;
                var url = $"gs://{data.Id.Replace(gen, "")}";
                var operationId = data.Id.Split('/')[2];
                dynamic result = DocumentAI.GetOperationStatusAsync(operationId).GetAwaiter().GetResult();

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
                    }

                }
            }
          
            return Task.CompletedTask;
        }
    }
}
