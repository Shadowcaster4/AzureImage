using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;

        //private IConfigurationRoot _config = new ConfigurationBuilder().AddJsonFile(@".\ImageResizerConfig.json").Build();
        public BaseService()
        {
         
        _applicationConnectionString = Environment.GetEnvironmentVariable("ApplicationEnvironment") switch
            {
                "Local" => Environment.GetEnvironmentVariable("LocalStorageConnectionString"),// Environment.GetEnvironmentVariable("LocalStorageConnectionString"),
                "Azure" => Environment.GetEnvironmentVariable("AzureWebJobsStorage"),// Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "LocalAzure" => Environment.GetEnvironmentVariable("LocalAzureStorageConnectionString"), // Environment.GetEnvironmentVariable("LocalAzureStorageConnectionString"),
                _ => "Connection string Error",
            };         
          
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           
        }
    }
}
