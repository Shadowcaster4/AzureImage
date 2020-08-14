using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using ImageResizer.Database;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using ServiceStack;

namespace ImageResizer.Utilities
{
    public static  class Utilities
    {

        private static IConfigurationRoot _config = new ConfigurationBuilder().AddJsonFile(@".\AppSettings.json").Build();

        public static string ContainerRemoveKey = Environment.GetEnvironmentVariable("ContainerRemoveKey");

        public static IImageService GetImageService(string environment="",string connectionString ="")
        {
        
            if (environment.IsNullOrEmpty())
                environment = _config.GetSection("ApplicationEnvironment").Value;

            return environment switch
            {
                "Local" => new ImageServiceLocally(connectionString.IsNullOrEmpty()
                    ? _config.GetSection("LocalStorageConnectionString").Value
                    : connectionString),
                "LocalAzure" => new ImageService(connectionString.IsNullOrEmpty()
                    ? _config.GetSection("LocalAzureStorageConnectionString").Value
                    : connectionString),
                "Azure" => new ImageService(connectionString.IsNullOrEmpty()
                    ? _config.GetSection("AzureWebJobsStorage").Value
                    : connectionString),
            };

        }

        public static IDatabaseService GetDatabaseService(string dbConnString="",bool initialize=true)
        {
            if (dbConnString.IsNullOrEmpty())
                dbConnString = _config.GetSection("DatabaseConnectionString").Value; 

            if (!initialize)
                return new DatabaseService(dbConnString, false);

            return new DatabaseService(dbConnString, true);
        }

      

        public static HttpResponseMessage GetHttpResponseMessage_ReturnsStatusCodeAndMessage(HttpStatusCode statusCode,string ResponseMessage)
        {
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(ResponseMessage)
            };
        }
    }
}
