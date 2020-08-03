using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    public class ImageData
    {
        public int Id { get; set; }
        public string ImageName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Size { get; set; }
    }
}
