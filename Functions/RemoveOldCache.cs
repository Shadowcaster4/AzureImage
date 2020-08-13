using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ImageResizer.Entities;

namespace ImageResizer
{
    public static class RemoveOldCache
    {
        
        [FunctionName("RemoveOldCache")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RemoveOldCache/{container}")] HttpRequest req,
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
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "Provided container is invalid");
                }

                var cachedImagesDictionary = service.GetCachedImagesDictionary(containerService);
                bool flag = true;
                int DaysAfterImageCacheWillBeDeleted = Int32.Parse(Environment.GetEnvironmentVariable("DaysAfterImageCacheWillBeDeleted"))*-1;

                foreach (var item in cachedImagesDictionary)
                {
                    if (item.Value < DateTime.UtcNow.AddDays(DaysAfterImageCacheWillBeDeleted))
                        if (!service.DeleteCachedImage(item.Key,containerService))
                            flag = false;
                }

                if (!flag)
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.InternalServerError, "Something went wrong not all files could be deleted");
                }
                else
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.OK, "Old Cache was successfully removed");
                }                 

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.InternalServerError, "");
            }
         
            
            
        }
    }
}
