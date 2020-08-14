using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using ImageResizer.Database;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using ServiceStack;

namespace ImageResizer.Utilities
{
    public static  class Utilities
    {
        public static string ContainerRemoveKey = Environment.GetEnvironmentVariable("ContainerRemoveKey");
        public static IImageService GetImageService(string? environment)
        {
            if (environment.IsNullOrEmpty())
            {
                var x = Environment.GetEnvironmentVariable("ApplicationEnvironment");
                environment = x;
            }
             

            if (environment == "Local")
                return new ImageServiceLocally();
            else
                return new ImageService();
        }

        public static IDatabaseService GetDatabaseService(string? dbConnString)
        {
            if(dbConnString.IsNullOrEmpty())
                dbConnString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
            return  new DatabaseService(dbConnString);
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
