using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        
        protected readonly string _applicationConnectionString;
        
        public BaseService()
        {
        }

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
        }
    }
}
