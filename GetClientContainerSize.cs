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
using System.Reflection.Metadata;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace ImageResizer
{
    public static class GetClientContainerSize
    {
       
        [FunctionName("GetClientContainerSize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetClientContainerSize/{container}")] HttpRequest req,
            string container,
            ILogger log)
        {
            try
            {
                var resp = new HttpResponseMessage();
                IImageService service = new ImageService();
                if (!service.SetServiceContainer(container))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("Provided container is innvalid");
                    return resp;
                }

                double ContainerSizeInMiB = Math.Round(service.GetImagesDictionarySize().Sum(x => x.Value) / (1024f * 1024f), 2);
                resp.StatusCode = HttpStatusCode.OK;                               
                resp.Content = new StringContent(JsonConvert.SerializeObject(value: new { ContainerName = container ,ContainerSizeMiB = ContainerSizeInMiB }));
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
