using System;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;
        //protected readonly string _databaseConnectionString;

        public BaseService()
        {
            if(Environment.GetEnvironmentVariable("LocalStorageFlag")=="true")
            _applicationConnectionString = Environment.GetEnvironmentVariable("LocalStorageConnectionString");
            else
            _applicationConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           
        }
    }
}
