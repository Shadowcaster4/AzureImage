using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImageResizer.Entities;
using ServiceStack;

namespace ImageResizer
{
    public static class GetClientContainerSize
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [FunctionName("GetClientContainerSize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetClientContainerSize/{container}")] HttpRequest req,
            string container)
        {
           
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

                var sizeDictionary = new Dictionary<string,double>();
                sizeDictionary.Add(containerService.GetContainerName(),containerSizeInMiB);

                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, JsonConvert.SerializeObject(sizeDictionary));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
                       
        }
    }
}
