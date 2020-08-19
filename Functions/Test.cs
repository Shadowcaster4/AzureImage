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
using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageResizer.Services;
using ImageResizer.Database;
using ServiceStack.Logging;

namespace ImageResizer.Functions
{
  
    public class Test
    {
        
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
           log.LogCritical("dsada");
           log.LogError("dsada");
           log.LogDebug("dsada");
           log.LogInformation("dsada");

            return new OkObjectResult("xD");
        }
    }
}
