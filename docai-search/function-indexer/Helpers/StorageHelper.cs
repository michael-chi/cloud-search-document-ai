using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StorageSample.OAuth2;
using System.Linq;
using System.Collections.Generic;
namespace StorageSample
{
    public class StorageAPI
    {
        //===
        public static async Task<string> UploadAsync(string name, byte[] content)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var gcs = $"{configuration.integration.DocumentAI.gcs}";
            var gcs_project = $"{configuration.integration.DocumentAI.gcs_project}";
            var client = await CreateHttpClientAsync();

            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{gcs}/o?name={name}";
            var resp = await client.PostAsync(url, new ByteArrayContent(
                content
            ));
            var result = await resp.Content.ReadAsStringAsync();
            Console.Write(result);

            resp.EnsureSuccessStatusCode();

            return result;
        }
        public static string CreateCSV(KeyValuePair<string, string>[] kvs)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in kvs)
            {
                sb.Append($"\"{kv.Key}\",\"{kv.Value}\"").Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        private static async Task<KeyValuePair<string, string>[]> DownloadFormFieldsResultAsync(string mediaLink)
        {
            Func<string, dynamic, string> getText = (string text, dynamic textAnchor) =>
            {
                if (Object.ReferenceEquals(null, textAnchor) ||
                    Object.ReferenceEquals(null, textAnchor.textSegments))
                {
                    return "";
                }

                // First shard in document doesn't have startIndex property
                try
                {
                    var startIndex = int.Parse($"{textAnchor.textSegments[0].startIndex}") > 0 ? int.Parse($"{textAnchor.textSegments[0].startIndex}") : 0;
                    var endIndex = int.Parse($"{textAnchor.textSegments[0].endIndex}") > 0 ? int.Parse($"{textAnchor.textSegments[0].endIndex}") : 0;

                    return text.Substring(startIndex, endIndex - startIndex + 1).Replace("\n", "_");
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"[ERROR]Parsing error:{textAnchor.textSegments[0].startIndex} | {textAnchor.textSegments[0].endIndex}");
                    Console.WriteLine($"[ERROR]Parsing error:{JsonConvert.SerializeObject(textAnchor.textSegments)}");
                    Console.WriteLine($"-->{exp.Message}");
                    return "";
                }
            };

            var body = await DownloadObjectAsync(mediaLink);
            dynamic document = (dynamic)JObject.Parse(body);
            var text = (string)document.text;

            dynamic pages = (JArray)document.pages;
            List<KeyValuePair<string, string>> ret = new List<KeyValuePair<string, string>>();
            foreach (dynamic page in pages)
            {
                foreach (dynamic formField in page.formFields)
                {
                    if (!Object.ReferenceEquals(null, formField.fieldName.textAnchor) &&
                        !Object.ReferenceEquals(null, formField.fieldValue.textAnchor))
                    {
                        string fn = getText(text, (dynamic)formField.fieldName.textAnchor);
                        string fv = getText(text, (dynamic)formField.fieldValue.textAnchor);
                        ret.Add(new KeyValuePair<string, string>(fn, fv));
                    }
                }
            }
            return ret.ToArray();
        }
        public static async Task<KeyValuePair<string, string>[]> GetFormFieldsResultsAsync(string folder)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var url = $"https://storage.googleapis.com/storage/v1/b/{configuration.integration.DocumentAI.gcs}/o?prefix={folder}";
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(url);

            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            dynamic json = JObject.Parse(body);

            var docAiResults = (from JToken item in (JArray)json.items
                                select new
                                {
                                    Id = (string)item["id"],
                                    Bucket = (string)item["bucket"],
                                    Generation = (string)item["generation"],
                                    FileName = ((string)item["name"]).Split('/').Last(),
                                    MediaLink = (string)item["mediaLink"],
                                    SelfLink = (string)item["selfLink"]
                                }).ToArray();

            List<KeyValuePair<string, string>> ret = new List<KeyValuePair<string, string>>();
            foreach (dynamic docAiResult in docAiResults)
            {
                var fields = await DownloadFormFieldsResultAsync(docAiResult.MediaLink);
                ret.AddRange(fields);
            }
            var test = ret.Select(r => r.Key.ToLower().IndexOf("tempa") >= 0 ? $"{r.Key}={r.Value}": "").ToArray();
            
            return ret.ToArray();
        }

        //===
        private static string ConstructMetadataLink(string objectId /* /{bucket_name}/{object_name} */, bool metadataOnly)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            Console.WriteLine(objectId);
            var bucket = objectId.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
            objectId = objectId.StartsWith("/") ?
                                        objectId.Replace("/" + bucket + "/", "") :
                                        objectId.Replace(bucket + "/", "");
            var encodedObjectId = Uri.EscapeDataString(objectId);
            var url = metadataOnly ?
                            $"https://storage.googleapis.com/storage/v1/b/{bucket}/o/{encodedObjectId}?alt=json" :
                            $"https://storage.googleapis.com/download/storage/v1/b/{bucket}/o/{encodedObjectId}?alt=media";

            return url;
        }

        public static async Task<string> GetDocumentAIResultsAsync(string folder)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var url = $"https://storage.googleapis.com/storage/v1/b/{configuration.integration.DocumentAI.gcs}/o?prefix={folder}";
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(url);

            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            dynamic json = JObject.Parse(body);

            var docAiResults = (from JToken item in (JArray)json.items
                                select new
                                {
                                    Id = (string)item["id"],
                                    Bucket = (string)item["bucket"],
                                    Generation = (string)item["generation"],
                                    FileName = ((string)item["name"]).Split('/').Last(),
                                    MediaLink = (string)item["mediaLink"],
                                    SelfLink = (string)item["selfLink"]
                                }).ToArray();

            List<string> ret = new List<string>();
            foreach (dynamic docAiResult in docAiResults)
            {
                string content = await DownloadDocumentAIOcrTextResultAsync(docAiResult.MediaLink);
                ret.Add(content);
            }
            return string.Join(Environment.NewLine, ret.ToArray());
        }
        public static async Task<string> DownloadAsync(string objectId, bool metadataOnly)
        {
            var mediaLink = ConstructMetadataLink(objectId, metadataOnly);

            Console.WriteLine($"MediaLink={mediaLink}");
            var result = await DownloadObjectAsync(mediaLink);

            return result;
        }
        private static async Task<string> DownloadObjectAsync(string mediaLink)
        {
            var client = await CreateHttpClientAsync();
            var resp = await client.GetAsync(mediaLink);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            return body;
        }
        public static async Task<string> DownloadDocumentAIOcrTextResultAsync(string mediaLink)
        {
            var body = await DownloadObjectAsync(mediaLink);
            var text = (string)JObject.Parse(body)["text"];
            return text;
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