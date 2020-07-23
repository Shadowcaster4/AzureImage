using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ImageResizer.Services.Interfaces;
using ImageResizer.Services;

namespace ImageResizer
{
    public static class GetImagesDictionary
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        [FunctionName("GetImagesDictionary")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Dictionary/{container}")] HttpRequest req,
            string container,
            ILogger log)
        {
            IImageService service = new ImageService(BLOB_STORAGE_CONNECTION_STRING);
            service.SetServiceContainer(container);
            var x = service.GetCachedImagesDictionary();

            return new OkObjectResult(x);
        }
    }
}
