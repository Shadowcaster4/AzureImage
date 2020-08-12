using System;
using System.Collections.Generic;
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
                environment = Environment.GetEnvironmentVariable("ApplicationEnvironment");

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
    }
}
