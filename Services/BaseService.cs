using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Services
{
    public abstract class BaseService
    {
        protected readonly string _applicationConnectionString;

        public BaseService(string applicationConnectionString)
        {
            _applicationConnectionString = applicationConnectionString;
        }
    }
}
