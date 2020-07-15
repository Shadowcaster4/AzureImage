using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Web.Http;

namespace ImageResizer
{
    public static class Remove
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        [FunctionName("Remove")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Remove")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a remove request.");

                var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);
                var result = blobServiceClient.GetBlobContainerClient(req.Form["containerToDelete"]).Exists();

                if (result)
                {
                    blobServiceClient.DeleteBlobContainer(req.Form["containerToDelete"]);
                    return new OkObjectResult("success");
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new BadRequestErrorMessageResult("Something went wrong");
            }

            return new BadRequestErrorMessageResult("Specified image doesn't exist");
        }
    }
}

