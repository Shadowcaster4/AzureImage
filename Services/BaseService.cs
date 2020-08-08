using System;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;
        

        public BaseService()
        {

            _applicationConnectionString = Environment.GetEnvironmentVariable("ApplicationEnvironment") switch
            {
                "Local" => Environment.GetEnvironmentVariable("LocalStorageConnectionString"),
                "Azure" => Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "LocalAzure" => Environment.GetEnvironmentVariable("LocalAzureStorageConnectionString"),
                _ => "Connection string Error",
            };         
          
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           
        }
    }
}
