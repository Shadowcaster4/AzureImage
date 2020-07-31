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
        public string WatermarkString { get ;private set; }

        public bool CorrectParameterString { get; private set; }
        public bool WatermarkPresence { get; private set; }

        private string [] Parameters { get; set; }

        public QueryParameterValues(string queryParameter)
        {
            Parameters = queryParameter.Split(',');
            CorrectParameterString = true;
            WatermarkPresence = true;

            switch (Parameters.Length)
            {
                case 0:  case 1: default:
                    Width = 0;
                    Height = 0;
                    Padding = false;
                    WatermarkString = "";
                    CorrectParameterString = false;
                    break;

                case 2:
                    Width = Int32.Parse(Parameters[0]);
                    Height = Int32.Parse(Parameters[1]);
                    break;

                case 3:
                    Width = Int32.Parse(Parameters[0]);
                    Height = Int32.Parse(Parameters[1]);

                    if (Parameters[2].Length>1)
                    {
                        Padding = false;
                        WatermarkString = Parameters[2];
                    }

                    if (Parameters[2].Length == 1 && Parameters[2] == "1")
                    {
                        Padding = true;
                        WatermarkString = "";
                    }          
                    break;

                case 4:
                    Width = Int32.Parse(Parameters[0]);
                    Height = Int32.Parse(Parameters[1]);
                    WatermarkString = Parameters[3];
                    Padding = false;
                    if (Parameters[2].Length == 1 && Parameters[2] == "1")
                        Padding = true; 
                    break;
                                    
            }

            
          
        }

        public void SetWatermarkPresence(bool presence)
        {
            WatermarkPresence = presence;
        }
    }
}
