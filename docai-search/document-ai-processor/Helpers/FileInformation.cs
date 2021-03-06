using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Dynamic;
using System.Collections.Generic;
using System.Runtime;
namespace cloud_search_fs
{
    public class FileInformationExtractor
    {
        /*
        Reserved for future in case we need different file extractors - PDF, Images...etc. This is how we're to get extractors for different file types
        */
        private string ExtraxtFileContent(string filePath){
            var extractorType = Type.GetType($"cloud_search_fs.FileInfoExtraxtors.PDFExtraxtor");
            var extraxtor = extractorType.GetMethod("Extract");
            var result = extraxtor.Invoke(null, new object []{
                filePath
            });
            if(result != null){
                return (string)result;
            }else{
                return string.Empty;
            }
        }
        public dynamic[] CollectFileInformation(string folder){
            var results = new List<dynamic>();
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                dynamic info = new {
                    Name = fi.Name,
                    FullName = fi.FullName,
                    CreateTime = fi.CreationTimeUtc,
                    UpdateTime = fi.LastWriteTimeUtc
                    //InlineContent = ExtraxtFileContent(fi.FullName)   //  We don't need this since content will be extracted by Document AI batch processor API
                };
                results.Add(info);
            }
            return results.ToArray();
        }
    }
}