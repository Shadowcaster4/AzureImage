using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    public class CloudFileInfo
    {
        public long Size { get; set; }
        public DateTimeOffset Date { get; set; }

        public CloudFileInfo(long size, DateTimeOffset date)
        {
            Size = size;
            Date = date;
        }
    }
}
