using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StorageSample.OAuth2;
using System.Linq;
namespace StorageSample
{
    public class ConfigHelper
    {
        private static string GetApplicationRoot()
        {
            var dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.
                                                GetExecutingAssembly().CodeBase);
            if (dir.StartsWith("file:///"))
            {
                dir = dir.Replace("file://", "");

            }
            else if (dir.StartsWith("file://"))
            {
                dir = dir.Replace("file:/", "");
            }else{
                dir = dir.Replace("file:","");
            }
            return dir;
        }
        public static string GetFilePath(string file)
        {
            var root = GetApplicationRoot();
            return Path.Combine(root, file);
        }
        public static dynamic ReadAppSettings()
        {
            var path = GetFilePath("appsettings.json");
            Console.WriteLine($"Reading Configuration File:{path}");

            dynamic configuration = JObject.Parse(File.ReadAllText(path));

            return configuration;
        }
    }
}