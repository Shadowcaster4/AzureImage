using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageResizer.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(ImageResizer.Startup))]
namespace ImageResizer
{
    public class Startup:FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            var logFileConfig = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"log4net.config") ;
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddLog4Net(logFileConfig);
            builder.Services.AddSingleton(loggerFactory);
            builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
           


        }

      
    }
}
