using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    public class CloudFileInfo
    {
        public long Size { get; set; }
        public DateTime Date { get; set; }

        public CloudFileInfo(long size, DateTime date)
        {
            Size = size;
            Date = date;
        }
        

    }
}
