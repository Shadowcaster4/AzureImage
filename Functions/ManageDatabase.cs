using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ImageResizer.Functions
{
    public static class ManageDatabase
    {
        [FunctionName("ManageDatabase")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ManageDatabase/{container}/{secKey}")] HttpRequest req,
            string container,
            string secKey)
        {
            var log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            try
            {
                IImageService service = Utilities.Utilities.GetImageService();
                IContainerService containerService = new ContainerClass(container);

                if (service.GetImageSecurityHash(containerService.GetContainerName(), Utilities.Utilities.ContainerRemoveKey) != secKey)
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Provided secKey is invalid");


                if (!service.CheckIfContainerExists(containerService))
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Provided container is invalid");

                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();

                databaseService.CreateTableIfNotExists();
                databaseService.RestoreDataForContainer(service, containerService);
                databaseService.CompareAndCorrectDbDataForContainer(service, containerService);


                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, $"Table data for container  was successfully restored");
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.InternalServerError, "");
            }
        
        }
    }
}
