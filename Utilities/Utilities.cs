using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ImageResizer.Database;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ServiceStack;

namespace ImageResizer.Utilities
{
    public static class Utilities
    {
        private static readonly string xfff = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "AppConfig.json");
        private static readonly IConfigurationRoot _config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"Utilities\AppConfig.json")).Build();
          
        public static readonly string ContainerRemoveKey = _config.GetSection("ContainerRemoveKey").Value;
        public static readonly int DaysAfterImageCacheWillBeDeleted = Int32.Parse(_config.GetSection("DaysAfterImageCacheWillBeDeleted").Value);

        

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

        public static string GetStorageConnectionString()
        {
            return _config.GetSection("LocalStorageConnectionString").Value;
        }

        public static IDatabaseService GetDatabaseService(string dbConnString="",bool initialize=true)
        {
            if (dbConnString.IsNullOrEmpty())
                dbConnString = _config.GetSection("DatabaseConnectionString").Value; 

            if (!initialize)
                return new DatabaseService(dbConnString, false);

            return new DatabaseService(dbConnString, true);
        }

      

        public static HttpResponseMessage GetHttpResponseMessage_ReturnsStatusCodeAndMessage(HttpStatusCode statusCode,string responseMessage)
        {
            if (!(responseMessage.StartsWith("{") && responseMessage.EndsWith("}") ||
                  responseMessage.StartsWith("[") && responseMessage.EndsWith("]")))
                responseMessage = JsonConvert.SerializeObject(value: responseMessage);

            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseMessage) 
                    { Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                    }
            };
        }

        

        public static HttpResponseMessage GetImageHttpResponseMessage(MemoryStream tmpImg,string imageName,string imageExtension)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(tmpImg.GetBuffer());
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = imageName
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + imageExtension);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = new TimeSpan(14, 0, 0, 0)
            };
            return response;
        }
    }
}
