using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace cloud_search_indexer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.UseUrls("http://*:8080");
                });
                // .ConfigureAppConfiguration(cfgBuilder =>{
                //     cfgBuilder.AddEnvironmentVariables()
                //                 .AddJsonStream(new System.IO.MemoryStream(
                //                     System.Text.Encoding.UTF8.GetBytes(
                //                         Environment.GetEnvironmentVariable("appsettings.json")
                //                     )
                //                 ));
                // });
    }
}
