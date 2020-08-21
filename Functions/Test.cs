using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using log4net;
using log4net.Config;
using System.Reflection;
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
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageResizer.Services;
using ImageResizer.Database;
using Microsoft.ApplicationInsights.Extensibility;
using ServiceStack.Logging;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace ImageResizer.Functions
{
    
    public class Test
    {

       


        [FunctionName("Test")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

           // TelemetryConfiguration.Active.InstrumentationKey = "";

            //var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
           // XmlConfigurator.Configure(logRepository,
            //    new FileInfo(Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"log4net.config")));
            
            log.Debug("dajesz2");
            //new Microsoft.ApplicationInsights.TelemetryClient().Flush();

            return new OkObjectResult("xD");
        }
    }
}
