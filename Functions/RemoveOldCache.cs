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
using SixLabors.ImageSharp.Drawing;

namespace ImageResizer
{
    public static class RemoveOldCache
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [FunctionName("RemoveOldCache")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RemoveOldCache/{container}")] HttpRequest req,
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
                int daysAfterImageCacheWillBeDeleted = Int32.Parse(Environment.GetEnvironmentVariable("DaysAfterImageCacheWillBeDeleted"));
              
                

                bool flag = service.RemoveOldCache(containerService, daysAfterImageCacheWillBeDeleted);


                if (!flag)
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.InternalServerError, "Something went wrong, not all files could be deleted");

                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, "Old Cache was successfully removed");

            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.InternalServerError, "");
            }
         
            
            
        }
    }
}
