using Google.Events.Protobuf.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Framework.GcfEvents;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using StorageSample.OAuth2;
using Newtonsoft.Json;
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
namespace StorageSample
{
    public class Function : ICloudEventFunction<StorageObjectData>
    {
        public Task HandleAsync(CloudEvent cloudEvent, StorageObjectData data, CancellationToken cancellationToken)
        {
            if(cloudEvent.Type == "google.cloud.storage.object.v1.finalized"){
                //kalschi-docai-2/completed/1356538610275523562/0/01 office轉pdf檔案_純中文文字敘述r1.pdf/1610850507909609
                var gen = "/" + data.Generation;
                var url = $"gs://{data.Id.Replace(gen,"")}";
                var operationId = data.Id.Split('/')[2];
            }
            Console.WriteLine($"CloudEvent type: {JsonConvert.SerializeObject(cloudEvent)}");
            Console.WriteLine($"Storage bucket: {data}");
            Console.WriteLine($"Storage object name: {data.Name}");
            return Task.CompletedTask;
        }
    }
}
