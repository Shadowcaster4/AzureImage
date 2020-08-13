using Dapper;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SQLite;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ImageResizer.Utilities;

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
                IImageService service =
                    Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

                IContainerService containerService = new ContainerClass(req.Form["container"]);

                resp.StatusCode = HttpStatusCode.Forbidden;

                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService(null);

                if (!service.CheckIfContainerExists(containerService))
                {
                    resp.StatusCode = HttpStatusCode.NotFound;
                    resp.Content = new StringContent("Provided container is invalid");
                    return resp;
                }

                switch (req.Form["objectToDelete"])
                {
                    case "container":
                        if (service.GetImageSecurityHash(containerService.GetContainerName(), Utilities.Utilities.ContainerRemoveKey) != req.Form["secKey"])
                            break;
                        if (service.CheckIfContainerExists(containerService))
                        {
                            service.DeleteClientContainer(containerService);
                            databaseService.DeleteClientContainer(containerService);

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
                        if(service.GetImageSecurityHash(req.Form["container"],req.Form["imageName"]) != req.Form["secKey"])
                            break;
                       
                        if(service.DeleteImageDirectory(req.Form["imageName"]))
                        { 
                            databaseService.DeleteImages(req.Form["imageName"], req.Form["container"]);
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
                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]) != req.Form["secKey"])
                            break;

                        var requestedParameters = new QueryParameterValues(req.Form["imageParameters"]);

                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]).Substring(0, 4) == requestedParameters.WatermarkString)
                            requestedParameters.SetWatermarkPresence(false);

                        if (service.DeleteCachedImage(service.GetImagePathResize(requestedParameters, req.Form["imageName"])))
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
                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]) != req.Form["secKey"])
                            break;         

                        if (service.DeleteLetterDirectory(req.Form["imageName"],databaseService.dbConnection2))
                        {
                            databaseService.DeleteLetterDirectory(req.Form["imageName"], req.Form["container"]);
                            resp.StatusCode = HttpStatusCode.OK;
                            resp.Content = new StringContent("Requested letter directory is gone");
                        
                        }
                        else
                        {
                            resp.StatusCode = HttpStatusCode.NotFound;
                            resp.Content = new StringContent("Requested letter directory doesn't exists");
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

