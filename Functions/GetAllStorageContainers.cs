using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using ImageResizer.Services.Interfaces;
using ImageResizer.Services;
using System.Net.Http.Headers;
using System.Net;
using System.Linq;
using Azure;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography.X509Certificates;
using ImageResizer.Entities;

namespace ImageResizer.Functions
{

    public static class GetAllStorageContainers
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [FunctionName("GetAllStorageContainers")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAllStorageContainers")] HttpRequest req)
        {
         
            try
            {
                IImageService service = Utilities.Utilities.GetImageService();

                List<string> containers = service.GetBlobContainers();
               
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.OK, JsonConvert.SerializeObject(
                        containers
                    
                    ));

            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
           
        }
    }
}
