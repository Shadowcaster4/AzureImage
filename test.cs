using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ImageResizer.Services;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using ImageResizer.Models;
using System.Collections.Generic;
using System.Linq;

namespace ImageResizer
{
    public struct FileInfo
    {
        public int Size { get; set; }
        public string Path { get; set; }

        public FileInfo(string path, int size)
        {
            Size = size;
            Path = path;
        }
    }
    
    public class test
    {


        public static Dictionary<string, FileInfo> GetLocalFiles(Dictionary<string, FileInfo> myFiles,string dirPath)
        {
            string[] files = Directory.GetFiles(dirPath, "*.*");
            string[] subDirs = Directory.GetDirectories(dirPath);

            foreach (string file in files)
            {
                myFiles.Add(Path.GetFileName(file), new FileInfo(Path.GetFullPath(file),file.Length));
            }
            var dictionary = new Dictionary<string, FileInfo>();
            foreach (string subDir in subDirs)
            {
                GetLocalFiles(dictionary , subDir);
            }
            return myFiles.Concat(dictionary).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
        }



        [FunctionName("test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequest req,
            
            ILogger log)
        {
             var service = new ImageService(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            //  var imageFromHttp = req.Form.Files.GetFile(req.Form.Files[0].Name);


             service.SetServiceContainer("nowyszef");
            // service.UploadImage(imageFromHttp.OpenReadStream(), "nowykontener", "rzecz.jpg");
            // var downloadedImage =  service.DownloadImageFromStorageToStream("test3.jpg");
            //// var image = service.MutateImage(downloadedImage, 600, 300, true);
            //   service.SaveImage(downloadedImage, "test45.jpg");
            //  var resizedImage = service.MutateImage(downloadedImage, 400, 400, false);
            //service.SaveImage(resizedImage, "poprawny2.jpg");
            /* using(var fileStream = System.IO.File.OpenRead(@"C:\Users\Tanatos\Documents\test2.jpg"))
             {
                 service.UploadImage(fileStream, "nowykontener", "test2.jpg");
             }
            */

            //   var x = new QueryParameterValues(parameters);

            
            
            //string directoryPath = @"C:\Users\Tanatos\Documents\import\import";

           // string[] files = Directory.GetFiles(dirPath, "*.*");
           // string[] subDirs = Directory.GetDirectories(directoryPath);
            // filePaths = Directory.GetFiles(directoryPath, "*.*");
           // var myDic = new Dictionary<string, FileInfo>();

           // var x = GetLocalFiles(myDic, directoryPath);


            return new OkObjectResult(service.GetImagesDictionarySize());

            
            /*pobieranie pliku ladne do prz\egladarki
            var myImg= service.MutateImage(downloadedImage, 400, 400, false);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(myImg.GetBuffer());
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = "resized.jpg"
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return response;
           */

        }
    }
}
