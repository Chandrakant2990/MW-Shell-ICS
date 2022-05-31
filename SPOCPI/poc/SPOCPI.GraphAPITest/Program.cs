
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Shell.SPOCPI.Common;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SPOCPI.GraphAPITest
{
    class Program
    {
        static string clientId = ""; //Enter Client ID
        static string tenantId = ""; //Enter Tenant ID
        static string clientSecret = ""; //Enter Client Secret
        static IConfidentialClientApplication confidentialClientApplication;
        /// <summary>
        /// Main method of Program
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {

            try
            {
                var ExpirationDateTime = Convert.ToDateTime("2022-01-01T23:39:16.0511696Z").ToUniversalTime();
                for (int i = 0; i < 10000; i++)
                {
                    ExpirationDateTime = ExpirationDateTime.AddMinutes(1);
                    var startLog = "Graph call started at " + DateTime.Now;
                    await UploadToBlob("log2.txt", "graphlogs", startLog);
                    confidentialClientApplication = ConfidentialClientApplicationBuilder
             .Create(clientId)
             .WithTenantId(tenantId)
             .WithClientSecret(clientSecret)
             .Build();
                    var scopes = new string[] { "https://graph.microsoft.com/.default" };
                    AuthenticationResult result = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();
                    var httpClient = new HttpClient();

                    var httpRequest = new HttpRequestMessage(HttpMethod.Patch,
                    "https://graph.microsoft.com/v1.0/subscriptions/2de7f3ab-0ff1-4c10-ad94-0abb47db0a7a");
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpRequest.Content = JsonContent.Create(new
                    {
                        ExpirationDateTime = ExpirationDateTime,
                    });
                    var response = await httpClient.SendAsync(httpRequest);
                    var data = response.Content.ReadAsStringAsync().Result;
                    var endLog = "Graph call ended after updating time to " + ExpirationDateTime + " with status code " + response.StatusCode.ToString() + " at " + DateTime.Now + " after counter " + i + " with response json " + data +" with headers "+response.Headers;
                    await UploadToBlob("log2.txt", "graphlogs", endLog);
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                await UploadToBlob("log2.txt", "graphlogs", ex.ToString());
            }
        }
        public static async Task<string> UploadToBlob(string filetoUpload, string azure_ContainerName, string log)
        {
            string storageAccount_connectionString = "";
            CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(storageAccount_connectionString);
            CloudBlobClient blobClient = mycloudStorageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(azure_ContainerName);
            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(filetoUpload);
            string textContent = string.Empty;
            var Mstream = new MemoryStream();

            bool blobExists = await (cloudBlockBlob?.ExistsAsync()).ConfigureAwait(false);
            if (blobExists)
            {
                await cloudBlockBlob.DownloadToStreamAsync(Mstream).ConfigureAwait(false);
                Mstream.Position = 0;
            }
            textContent = string.Empty;
            using (var reader = new StreamReader(Mstream))
            {
                textContent = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            textContent += Environment.NewLine;
            textContent += log;

            await cloudBlockBlob.UploadTextAsync(textContent).ConfigureAwait(false);
            return textContent;
        }
    }
}
