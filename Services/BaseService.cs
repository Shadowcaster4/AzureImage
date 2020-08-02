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
           // _applicationConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _applicationConnectionString = "UseDevelopmentStorage=true";
          //  _databaseConnectionString = @"Data Source=.\ImageResizerDB.db;Version=3;";

        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
           // _databaseConnectionString = databaseConnectionString;
        }
    }
}
