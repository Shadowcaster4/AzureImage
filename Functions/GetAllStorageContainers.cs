using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using ImageResizer.Services.Interfaces;
using ImageResizer.Services;
using System.Net.Http.Headers;
using System.Net;
using System.Linq;
using Azure;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography.X509Certificates;

namespace ImageResizer.Functions
{
    public static class GetAllStorageContainers
    {
        [FunctionName("GetAllStorageContainers")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAllStorageContainers")] HttpRequest req,
         
            ILogger log)
        {
            try
            {
                var resp = new HttpResponseMessage();
                IImageService service = Utilities.Utilities.GetImageService();

                var containers = service.GetBlobContainers();
               
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, JsonConvert.SerializeObject(value: new
                    {
                        ContainersNames = containers
                    }));

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
           
        }
    }
}
