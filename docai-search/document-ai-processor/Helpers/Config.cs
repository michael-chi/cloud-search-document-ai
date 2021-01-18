using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace cloud_search_fs
{
    public class ConfigHelper{
        public static dynamic ReadAppSettings(){
            dynamic configuration = JObject.Parse(File.ReadAllText("./appsettings.json"));
            
            return configuration;
        }
    }
}