using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CloudNative.CloudEvents;
namespace cloud_search_indexer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CloudSearchIndexingController : ControllerBase
    {
        

        private readonly ILogger<CloudSearchIndexingController> _logger;

        public CloudSearchIndexingController(ILogger<CloudSearchIndexingController> logger)
        {
            _logger = logger;
        }

        //  Event Json Schema
        //  * https://googleapis.github.io/google-cloudevents/jsonschema/google/events/cloud/storage/v1/StorageObjectData.json
        //  * https://github.com/cloudevents/sdk-csharp/blob/master/src/CloudNative.CloudEvents/CloudEvent.cs
        //  * https://googleapis.github.io/google-cloudevents/jsonschema/google/events/cloud/storage/v1/StorageObjectData.json
        [HttpPost]
        public void IndexFile([FromBody]CloudEvent cloudEvent){
            // var logEntryData = CloudEventConverters.ConvertCloudEventData<LogEntryData>(cloudEvent);
            // var tokens = logEntryData.ProtoPayload.ResourceName.Split('/');
            // var bucket = tokens[3];
            // var name = tokens[5];
            string eventType = cloudEvent.Type;
            if(eventType == "google.cloud.storage.object.v1.finalized"){
                
            }
            Console.WriteLine(cloudEvent);
        }
    }
}
