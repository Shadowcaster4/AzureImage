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
            string container)
        {
            var log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            try
            {
                IImageService service = Utilities.Utilities.GetImageService();
                IContainerService containerService = new ContainerClass(container);
                if (!service.CheckIfContainerExists(containerService))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "Provided container is invalid");
                }

                double containerSizeInMiB = Math.Round(service.GetImagesDictionaryPathAndSize(containerService).Sum(x => x.Value) / (1024f * 1024f), 2);
              
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, JsonConvert.SerializeObject(value: new { ContainerName = containerService.GetContainerName(), ContainerSizeMiB = containerSizeInMiB }));
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
                       
        }
    }
}
