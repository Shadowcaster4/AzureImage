using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImageResizer.Entities;

namespace ImageResizer
{
    public static class GetClientContainerSize
    {
       
        [FunctionName("GetClientContainerSize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetClientContainerSize/{container}")] HttpRequest req,
            string container,
            ILogger log)
        {
            try
            {
                var resp = new HttpResponseMessage();
                IImageService service =
                    Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));
                IContainerService containerService = new ContainerClass(container);
                if (!service.CheckIfContainerExists(containerService))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("Provided container is invalid");
                    return resp;
                }

                double ContainerSizeInMiB = Math.Round(service.GetImagesDictionarySize(containerService).Sum(x => x.Value) / (1024f * 1024f), 2);
                resp.StatusCode = HttpStatusCode.OK;                               
                resp.Content = new StringContent(JsonConvert.SerializeObject(value: new { ContainerName = containerService.GetContainerName() ,ContainerSizeMiB = ContainerSizeInMiB }));
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return resp;
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
                       
        }
    }
}
