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
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Provided container is invalid");
                    
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

                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "User container is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "User container doesnt exists");
                        }                           
                        break;

                    case "imageDirectory":
                        if(service.GetImageSecurityHash(req.Form["container"],req.Form["imageName"]) != req.Form["secKey"])
                            break;
                       
                        if(service.DeleteImageDirectory(req.Form["imageName"],containerService))
                        { 
                            databaseService.DeleteImage(req.Form["imageName"], containerService);
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested directory is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested directory doesnt exists");
                        }
                        break;
                    case "singleImage":
                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]) != req.Form["secKey"])
                            break;

                        var requestedParameters = new QueryParameterValues(req.Form["imageParameters"]);

                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]).Substring(0, 4) == requestedParameters.WatermarkString)
                            requestedParameters.SetWatermarkPresence(false);

                        if (service.DeleteCachedImage(service.GetImagePathResize(requestedParameters, req.Form["imageName"]),containerService))
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested image is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested file doesnt exists");
                        }
                        break;
                    case "letterDirectory":
                        if (service.GetImageSecurityHash(req.Form["container"], req.Form["imageName"]) != req.Form["secKey"])
                            break;         

                        if (service.DeleteLetterDirectory(req.Form["imageName"],containerService))
                        {
                            databaseService.DeleteLetterDirectory(req.Form["imageName"], containerService);
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested letter directory is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested letter directory doesn't exists");
                        }
                        break;
                    default:
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.BadRequest, "Invalid objectToDelete option");
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

