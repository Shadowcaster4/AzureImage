using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    public class LocalFileInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Date { get; set; }

        public LocalFileInfo(string name,long size, DateTime date)
        {
            Name = name;
            Size = size;
            Date = date;
        }
    }
}
