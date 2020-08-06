using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using ImageResizer.Entities;
using System.Data.SQLite;
using System.Linq;
using ImageResizer.Services.Interfaces;

namespace ImageResizer.Functions
{
  
    public class Test
    {
        
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            IImageService service = new ImageServiceLocally();

                   
           

            return new OkObjectResult(service.Test(service.GetImagePathUpload("xddd.jpg")));
        }
    }
}
