using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Web.Http;
using ImageResizer.Services;
using System.Net.Http;
using System.Net;

namespace ImageResizer
{
    public static class Remove
    {
        
        [FunctionName("Remove")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Remove")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var resp = new HttpResponseMessage();
                var service = new ImageService();

                if(!service.SetServiceContainer(req.Form["container"]))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("Provided container is innvalid");
                    return resp;
                }

                switch (req.Form["objectToDelete"])
                {
                    case "container":
                        if(service.CheckIfContainerExists(req.Form["container"]))
                            service.DeleteClientContainer(req.Form["container"]);
                        resp.StatusCode = HttpStatusCode.OK;
                        resp.Content = new StringContent("User container is gone");
                        break;
                    case "imageDirectory":
                        if(service.DeleteImageDirectory(req.Form["fileName"]))
                        {
                        resp.StatusCode = HttpStatusCode.OK;
                        resp.Content = new StringContent("Requested directory is gone");
                        }
                        break;
                    case "singleImage":
                        if (service.DeleteCachedImage(service.GetImagePathResize(req.Form["imageParameters"], req.Form["imageName"])))
                        {
                            resp.StatusCode = HttpStatusCode.OK;
                            resp.Content = new StringContent("Requested image is gone");
                        }
                        break;
                    default:
                        resp.StatusCode = HttpStatusCode.BadRequest; 
                        resp.Content = new StringContent("Invalid objectToDelete option");
                        break;

                }

                return resp;               
                
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            
        }
    }
}

