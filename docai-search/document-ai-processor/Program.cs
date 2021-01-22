using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;

namespace cloud_search_fs
{
    class Program
    {
        static void OnChangedHandler(object sender, FileSystemEventArgs e)
        {

        }
        static private void OnRenamedHandler(object source, RenamedEventArgs e)
        {

        }
        static private void SetupFileWatcher(string parentFolder)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(parentFolder);
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += new FileSystemEventHandler(OnChangedHandler);
            watcher.Renamed += new RenamedEventHandler(OnRenamedHandler);
        }

        static private void Scan(string parentFolder)
        {
            var extractor = new FileInformationExtractor();
            dynamic configuration = ConfigHelper.ReadAppSettings();
            var files = (dynamic[])extractor.CollectFileInformation($"{configuration.fileSystem.folderPath}");

            StorageAPI.EnsureStorageBuckets().Wait();
            foreach (dynamic file in files)
            {
                Console.WriteLine($"Extracting {file.Name}...");
                //  Send files to storage bucket for large file scanning
                var url = StorageAPI.SendToStorageBucketAsync(file.Name, file.FullName, File.ReadAllBytes(file.FullName)).GetAwaiter().GetResult();
                // var parsed = DocumentAI.DocumentProcessAsync(url).GetAwaiter().GetResult();
                var parsed = DocumentAI.LargeFormParserAsync(url).GetAwaiter().GetResult();
                // var parsed = DocumentAI.LargeOCRAsync(url).GetAwaiter().GetResult();
                
            }
            // var task = CloudSearchAPI.IndexAsync("002","002",new string []{"002","Michael","Test","DocAI"},"/Users/kalschi/Documents/codes/cloud-search/cloud-search-fs/001.txt","TEXT",
            //                                             DateTime.Now, DateTime.Now,"TEXT","thi is s test by Michael", "0.03");
            // task.Wait();

        }
        static void Main(string[] args)
        {
            dynamic configuration = ConfigHelper.ReadAppSettings();

            SetupFileWatcher($"{configuration.fileSystem.folderPath}");

            Scan($"{configuration.fileSystem.folderPath}");
            Console.WriteLine("Scanned, waiting inputs to exit...");
            Console.ReadLine();
        }
    }
}
