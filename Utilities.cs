using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;

namespace ImageResizer.Utilities
{
    public  class Utilities
    {
        public static IImageService GetImageService(string Environment)
        {
            if (Environment == "Local")
                return new ImageServiceLocally();
            else
                return new ImageService();
        }
    }
}
