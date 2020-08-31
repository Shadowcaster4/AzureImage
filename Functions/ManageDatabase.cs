using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace ImageResizer.Functions
{
    public static class ManageDatabase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [FunctionName("ManageDatabase")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ManageDatabase/{container}/{secKey}")] HttpRequest req,
            string container,
            string secKey)
        {
            
            try
            {
                IImageService service = Utilities.Utilities.GetImageService();
                IContainerService containerService = new ContainerClass(container);

                if (service.GetImageSecurityHash(containerService.GetContainerName(), Utilities.Utilities.ContainerRemoveKey) != secKey)
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Provided SecKey is invalid");


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
                Log.Error(e.Message);
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.InternalServerError, "");
            }
        
        }
    }
}
