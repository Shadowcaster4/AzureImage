using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageResizer
{
    public static class GetImagesDictionary
    {
        
        [FunctionName("GetImagesDictionary")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Dictionary/{container}")] HttpRequest req,
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
                        HttpStatusCode.BadRequest, "");
                }
               
                var cloudImages = service.GetBaseImagesDictionary(containerService);
              
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, JsonConvert.SerializeObject(value: cloudImages));
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
           
        }
    }
}
