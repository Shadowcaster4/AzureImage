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
using System.Net.Http;
using System.Net;

namespace ImageResizer
{
    public static class RemoveOldCache
    {
        
        [FunctionName("RemoveOldCache")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "RemoveOldCache/{container}")] HttpRequest req,
            string container,
            ILogger log)
        {
            try
            {
                var resp = new HttpResponseMessage();
                IImageService service = new ImageService();

                if(!service.SetServiceContainer(container))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("Provided container is innvalid");
                    return resp;
                }

                var cachedImagesDictionary = service.GetCachedImagesDictionary();
                bool flag = true;

                foreach (var item in cachedImagesDictionary)
                {
                    if (item.Value < DateTimeOffset.UtcNow.AddDays(-15))
                        if (!service.DeleteCachedImage(item.Key))
                            flag = false;
                }

                if (!flag)
                {
                    resp.StatusCode = HttpStatusCode.InternalServerError;
                    resp.Content = new StringContent("Something went wrong not all files could be deleted");
                    return resp;
                }
                else
                {
                    resp.StatusCode = HttpStatusCode.OK;
                    resp.Content = new StringContent("Old Cache was successfully removed");
                    return resp;
                }                 

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
         
            
            
        }
    }
}
