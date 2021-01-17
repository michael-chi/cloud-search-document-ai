using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
namespace cloud_search_fs.OAuth2
{
    public class OAuth2HeaderHelper
    {
        private static ServiceAccountCredential CloudSearchServiceAccountAuthz(string saEmail, string saCertPath, string password)
        {
            var certificate = new X509Certificate2(saCertPath, password, X509KeyStorageFlags.Exportable);
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
            var certificate = new X509Certificate2(saCertPath, password, X509KeyStorageFlags.Exportable);
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
            /*
            public object AuthExplicit(string projectId, string jsonPath)
        {
            // Explicitly use service account credentials by specifying 
            // the private key file.
            var credential = GoogleCredential.FromFile(jsonPath);
            var storage = StorageClient.Create(credential);
            // Make an authenticated API request.
            var buckets = storage.ListBuckets(projectId);
            foreach (var bucket in buckets)
            {
                Console.WriteLine(bucket.Name);
            }
            return null;
        }
        */
            var token = await DocumentAIServiceAccountAuthz(
                                                            saEmail,
                                                            certFile,
                                                            password
                                                        ).GetAccessTokenForRequestAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}