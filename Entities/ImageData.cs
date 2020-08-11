using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    [CompositeIndex(true, nameof(ImageName), nameof(ClientContainer))]
    public class ImageData
    {
        [AutoIncrement]
        public int Id { get; set; }
        [Required]
        public string ImageName { get; set; }
        [Required]
        public string ClientContainer { get; set; }
        [Required]
        public int Width { get; set; }
        [Required]
        public int Height { get; set; }
        [Required]
        public string Size { get; set; }
    }
}
