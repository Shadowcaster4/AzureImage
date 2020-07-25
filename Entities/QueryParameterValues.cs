using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Entities
{
    public class QueryParameterValues
    {
        public int Width { get ;private set; }
        public int Height { get ;private set; }
        public bool Padding { get ;private set; }

        public QueryParameterValues(string queryParameter)
        {
            int x = queryParameter.IndexOf(',');
            int y = queryParameter.LastIndexOf(',');
            Width = Int32.Parse(queryParameter.Substring(0, x));
            Height = Int32.Parse(queryParameter.Substring(x + 1, y - x - 1));
            Padding = Boolean.Parse(queryParameter.Substring(y + 1));
        }
    }
}
