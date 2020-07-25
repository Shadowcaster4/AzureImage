using ImageResizer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
                    resp.StatusCode = HttpStatusCode.NotFound;
                    resp.Content = new StringContent("Provided container is innvalid");
                    return resp;
                }

                switch (req.Form["objectToDelete"])
                {
                    case "container":
                        if(service.CheckIfContainerExists(req.Form["container"]))
                        {
                            service.DeleteClientContainer(req.Form["container"]);
                            resp.StatusCode = HttpStatusCode.OK;
                            resp.Content = new StringContent("User container is gone");
                        }
                        else
                        {                            
                            resp.StatusCode = HttpStatusCode.NotFound;
                            resp.Content = new StringContent("User container doesnt exists");
                        }                           
                        break;

                    case "imageDirectory":
                        if(service.DeleteImageDirectory(req.Form["imageName"]))
                        {
                        resp.StatusCode = HttpStatusCode.OK;
                        resp.Content = new StringContent("Requested directory is gone");
                        }
                        else
                        {
                        resp.StatusCode = HttpStatusCode.NotFound;
                        resp.Content = new StringContent("Requested directory doesnt exists");
                        }
                        break;
                    case "singleImage":
                        if (service.DeleteCachedImage(service.GetImagePathResize(req.Form["imageParameters"], req.Form["imageName"])))
                        {
                            resp.StatusCode = HttpStatusCode.OK;
                            resp.Content = new StringContent("Requested image is gone");
                        }
                        else
                        {
                        resp.StatusCode = HttpStatusCode.NotFound;
                        resp.Content = new StringContent("Requested file doesnt exists");
                        }
                        break;
                    case "letterDirectory":
                        if (service.DeleteLetterDirectory(req.Form["imageName"]))
                        {
                            resp.StatusCode = HttpStatusCode.OK;
                            resp.Content = new StringContent("Requested letter directory is gone");
                        }
                        else
                        {
                            resp.StatusCode = HttpStatusCode.NotFound;
                            resp.Content = new StringContent("Requested letter directory doesnt exists");
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

