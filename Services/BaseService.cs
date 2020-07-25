using System;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        //storage connection
        protected readonly string _applicationConnectionString;

        public BaseService()
        {
            _applicationConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
        }
    }
}
