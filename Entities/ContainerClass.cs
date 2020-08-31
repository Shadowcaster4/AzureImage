using System;
using ImageResizer.Services.Interfaces;

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
