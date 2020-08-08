using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImageResizer.Entities
{
    public class testClass
    {
        public static Dictionary<string, LocalFileInfo> GetLocalFiles2(Dictionary<string, LocalFileInfo> myFiles, string startLocation, int deepth)
        {
           // string[] files = Directory.GetFiles(startLocation, "*.*");
            string[] subDirs = Directory.GetDirectories(startLocation);

            /*
            foreach (string file in files)
            {
                myFiles.Add(file, new LocalFileInfo(Path.GetFileName(file), new FileInfo(Path.GetFullPath(file)).Length, File.GetCreationTime(Path.GetFullPath(file))));
            }
            */
            // var dictionary = new Dictionary<string, CloudFileInfo>();

            if(deepth==0)
            {
                string[] files = Directory.GetFiles(startLocation, "*.*");
                foreach (string file in files)
                {                    
                    FileInfo tmpFile = new FileInfo(Path.GetFullPath(file));
                    myFiles.Add(file, new LocalFileInfo(tmpFile.Name, tmpFile.Length, tmpFile.CreationTime));                    
                }
                return myFiles;
            }

            foreach (string dir in subDirs)
            {
                GetLocalFiles2(myFiles, dir,deepth-1);
            }
            return myFiles;
            //.Concat(dictionary).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
        }
    }
}
