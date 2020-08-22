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
using ServiceStack;

namespace ImageResizer
{
    public static class Remove
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [FunctionName("Remove")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Remove")] HttpRequest req)
        {
          
            try
            {

                IImageService service = Utilities.Utilities.GetImageService();
                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();

                string objectToDelete = req.Form["objectToDelete"];
                string containerName = req.Form["container"];
                string imageName = req.Form["imageName"];
                string imageParameters = req.Form["imageParameters"];
                string secKey = req.Form["secKey"];

                IContainerService containerService = new ContainerClass(containerName);


                if (!service.CheckIfContainerExists(containerService))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Provided container is invalid");
                    
                }

                switch (objectToDelete)
                {
                    case "container":
                        if (service.GetImageSecurityHash(containerService.GetContainerName(), Utilities.Utilities.ContainerRemoveKey) != secKey)
                            break;
                        if (service.CheckIfContainerExists(containerService))
                        {
                            service.DeleteClientContainer(containerService);
                            databaseService.DeleteClientContainer(containerService);

                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Client container is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Client container doesn't exist");
                        }                           
                        break;

                    case "imageDirectory":
                        if(service.GetImageSecurityHash(containerName,imageName) != secKey || imageName.IsNullOrEmpty())
                            break;
                       
                        if(service.DeleteImageDirectory(imageName,containerService))
                        { 
                            databaseService.DeleteImage(imageName, containerService);
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested directory is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested directory doesn't exist");
                        }
                        break;
                    case "singleImage":
                        if (service.GetImageSecurityHash(containerName, imageName) != secKey || imageName.IsNullOrEmpty())
                            break;

                        var requestedParameters = new QueryParameterValues(imageParameters);

                        if (service.GetImageSecurityHash(containerName, imageName).Substring(0, 4) == requestedParameters.WatermarkString)
                            requestedParameters.SetWatermarkPresence(false);

                        if (service.DeleteSingleCacheImage(service.GetImagePathResize(requestedParameters, imageName),containerService))
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested image is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested file doesn't exists");
                        }
                        break;
                    case "letterDirectory":
                        if (service.GetImageSecurityHash(containerName, imageName) != secKey || imageName.IsNullOrEmpty())
                            break;         

                        if (service.DeleteLetterDirectory(imageName,containerService))
                        {
                            databaseService.DeleteLetterDirectory(imageName, containerService);
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.OK, "Requested letter directory is gone");
                        }
                        else
                        {
                            return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                                HttpStatusCode.NotFound, "Requested letter directory doesn't exist");
                        }
                        break;
                    default:
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.BadRequest, "Invalid objectToDelete option");
                        break;

                }

                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.Forbidden, "");

            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
           

            
        }
    }
}

