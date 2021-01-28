using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
namespace StorageSample.OAuth2
{
    public class OAuth2HeaderHelper
    {
        private static ServiceAccountCredential CloudSearchServiceAccountAuthz(string saEmail, string saCertPath, string password)
        {
            var certificate = new X509Certificate2(ConfigHelper.GetFilePath(saCertPath), password, X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(saEmail)
               {
                   Scopes = new[]
                   {
                        "https://www.googleapis.com/auth/cloud_search",
                        "https://www.googleapis.com/auth/cloud_search.indexing",
                   }
               }.FromCertificate(certificate));
            return credential;
        }
        private static ServiceAccountCredential DocumentAIServiceAccountAuthz(string saEmail, string saCertPath, string password)
        {
            var certificate = new X509Certificate2(ConfigHelper.GetFilePath(saCertPath), password, X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(saEmail)
               {
                   Scopes = new[]
                   {
                        "https://www.googleapis.com/auth/cloud-platform",
                        "https://www.googleapis.com/auth/devstorage.full_control",
                        "https://www.googleapis.com/auth/devstorage.read_write"
                   }
               }.FromCertificate(certificate));
            return credential;
        }
        static public async Task UpdateCloudSearchOAuthHeaderAsync(HttpClient client, string saEmail, string certFile, string password)
        {
            var token = await CloudSearchServiceAccountAuthz(
                                                            saEmail,
                                                            certFile,
                                                            password
                                                        ).GetAccessTokenForRequestAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        static public async Task UpdateDocAIOAuthHeaderAsync(HttpClient client, string saEmail, string certFile, string password)
        {
            var token = await DocumentAIServiceAccountAuthz(
                                                            saEmail,
                                                            certFile,
                                                            password
                                                        ).GetAccessTokenForRequestAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}