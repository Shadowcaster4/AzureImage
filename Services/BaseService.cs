using System;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;
        

        public BaseService()
        {
            _applicationConnectionString = @"C:\Users\Tanatos\source\repos\import";
          /* 
            if(Environment.GetEnvironmentVariable("LocalStorageFlag")=="true")
            _applicationConnectionString = Environment.GetEnvironmentVariable("LocalStorageConnectionString");
            else
            _applicationConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
           */
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           
        }
    }
}
