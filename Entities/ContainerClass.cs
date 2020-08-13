using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageResizer.Services.Interfaces;
using ServiceStack;

namespace ImageResizer.Entities
{ 
    public class ContainerClass:IContainerService
    {
        private string ContainerName;
  
        public ContainerClass(string containerName)
        {
            ContainerName = containerName;
        }

        public string GetContainerName()
        {
            return ContainerName;
        }

      
    }
}
