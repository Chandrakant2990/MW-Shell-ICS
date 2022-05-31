using Shell.SPOCPI.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TestConsole
{
    public static class MockChange
    {
        public static GraphServiceClient gclient = GraphHelper.GetGraphClient("76775f48-19b3-4386-b7db-ce8acf08c644", "ebdaa46d-c34f-4721-a917-d3b44ad75ae4", "toN*-+EPB9]X25WQKQvyH02b/?PBz*fg");

        public static string GetDriveFromResource(string resource)
        {
            string[] tokens = resource.Split('/');

            return tokens[1];
        }

        public static string GetTokenFromDeltaUrl(string deltaurl)
        {
            string[] tokens = deltaurl.Split("token='");

            if (tokens.Length > 1)
            {
                string[] tokens2 = tokens[1].Split("')");

                return tokens2[0].Trim();
            }
            else
            {
                tokens = deltaurl.Split("token=");

                return tokens[1];
            }
        }

        //TODO implement another global lock mechanism. singleton works only in one instance function. 
        [Singleton("{subscriptionId}")]
        [FunctionName("SPChangesProcessor")]
        public static async System.Threading.Tasks.Task RunAsync()
        {
            //Configuration
            //GeneralConfiguration flowcfg = JsonConvert.DeserializeObject<GeneralConfiguration>(cfg1.ConfigurationData);

            //SharePointConfiguration spcfg = JsonConvert.DeserializeObject<SharePointConfiguration>(cfg2.ConfigurationData);

            //GraphServiceClient gclient = GraphHelper.GetGraphClient(spcfg.Tenant);

            //{ "subscriptionId":"0cd531f8-5c14-4b7e-85e0-e91921323590",
            //"clientState":"DTeam",
            //"subscriptionExpirationDateTime":"2019-02-14T12:00:02+00:00",
            //"resource":"drives/b!gmxJDOH2bUaOc_JDY0lFX0-BrwOkHvJCpv8vbLo6CZOr8YrOsB6VSYr4O3lGYWt2/root",
            //"resourceData":null,"tenantId":"bca9845a-75fa-4acb-8388-60079f93a6d6",
            //"changeType":"updated"}

            string deltatoken = null;//"MzslMjM0OyUyMzE7Mzs2MmQwMGZkNC0xNzhmLTRlMDctOTZhZS1iZTA5MDhmOTYwMGU7NjM2OTg0NDg0NDI5MzMwMDAwOzI5NjQ0NTkwNzslMjM7JTIzOyUyMzQ";


            string resource = "drives/b!Rw7nP7zzYEidCG7orV4Un1k7CxKzaXNAqBsTVEGd71nUD9BijxcHTpauvgkI-WAO/root";

            IDriveItemDeltaCollectionPage response = null;

            // Pages of changes from the document library 
            try
            {
                //drives / b!8UPd1gqKsk2gQoM1zLvb8JZPxsnCjANFnWsj4pXKj0Jq9JMg34RtQYxA3ffUT1WR / root / delta
                if (deltatoken != null)
                {
                    response = await gclient.Drives[GetDriveFromResource(resource)].Root.Delta(deltatoken)
                        .Request()
                        .Select("content.downloadUrl,id,name,deleted,createdDateTime,lastModifiedDateTime,webUrl,cTag,eTag,size,parentReference,file,folder,sharepointIds,location")
                        //.Expand("permissions")
                        .GetAsync();
                }
                else
                {
                    response = await gclient.Drives[GetDriveFromResource(resource)].Root.Delta()
                        .Request()
                        .Select("content.downloadUrl,id,name,deleted,createdDateTime,lastModifiedDateTime,webUrl,cTag,eTag,size,parentReference,file,folder,sharepointIds,location")
                        //.Expand("permissions")
                        .GetAsync();
                }

                Console.WriteLine("Delta Found " + response.Count + " changes.");
                if (response.AdditionalData.ContainsKey("@odata.deltaLink"))
                {
                    deltatoken = GetTokenFromDeltaUrl((string)response.AdditionalData["@odata.deltaLink"]);
                }
                Console.WriteLine(deltatoken);
            }
            catch
            {
            }
        } 
    }
}
