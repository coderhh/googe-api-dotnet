namespace GoogleAPI.AzureFunction
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Google.Apis.Discovery.v1;
    using global::Google.Apis.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public static class Google
    {
        private static readonly string GoogleApiKey = System.Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        [FunctionName("Google")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, [Queue("outqueue"), StorageAccount("AzureWebJobsStorage")] ICollector<string> msg,
            [CosmosDB(
                databaseName: "googleapi",
                collectionName: "googleapi",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            if (!string.IsNullOrEmpty(name))
            {
                msg.Add(name);
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    name = name
                });
            }

            var service = new DiscoveryService(new BaseClientService.Initializer
            {
                ApplicationName = "Discovery Sample",
                ApiKey = GoogleApiKey
            });

            var result = await service.Apis.List().ExecuteAsync();

            if (result != null)
            {
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    Apis = result.Items.ToList()
                });
            }
            

            return result != null
                ? (ActionResult)new OkObjectResult(result.Items.Select(x => x.Name))
                : new BadRequestObjectResult("Unable to get api list from google");
        }
    }
}
