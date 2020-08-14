using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;

        private IConfigurationRoot _config = new ConfigurationBuilder().AddJsonFile(@".\AppSettings.json").Build();
        public BaseService()
        {
         
        _applicationConnectionString = Environment.GetEnvironmentVariable("ApplicationEnvironment") switch
            {
                "Local" => _config.GetSection("LocalStorageConnectionString").Value,// Environment.GetEnvironmentVariable("LocalStorageConnectionString"),
                "Azure" => _config.GetSection("AzureWebJobsStorage").Value,// Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "LocalAzure" => _config.GetSection("LocalAzureStorageConnectionString").Value, // Environment.GetEnvironmentVariable("LocalAzureStorageConnectionString"),
                _ => "Connection string Error",
            };         
          
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           
        }
    }
}
