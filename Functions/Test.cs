using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using ImageResizer.Entities;
using System.Data.SQLite;
using System.Linq;
using ImageResizer.Services.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageResizer.Services;
using ImageResizer.Database;

namespace ImageResizer.Functions
{
  
    public class Test
    {
        
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            /*
            IImageService service;
            if (Environment.GetEnvironmentVariable("ApplicationEnvironment") == "Local")
                service = new ImageServiceLocally();
            else
                service = new ImageService();
            Dictionary<string, LocalFileInfo> testDictionary = new Dictionary<string, LocalFileInfo>();


            // var x = new database();


            var x = Environment.GetEnvironmentVariable("DatabaseConnectionString");
            x = x.Substring(x.IndexOf("."), x.IndexOf(';')-x.IndexOf("."));

            var xyz = new FileInfo(x);
            xyz.CopyTo(x +".Bak",true);
            
            File.Exists(x);
            var gg =new FileInfo(x);
            var yy = gg;

            #region napotem
            //var xyz = ImageServiceLocally.GetLocalFiles(testDictionary, @"E:\imageresizer");

            /*
            string[] dirs = Directory.GetDirectories(@"E:\imageresizer", "*", SearchOption.AllDirectories);
            var files = Directory.GetFiles(@"E:\imageresizer", "*", SearchOption.AllDirectories);
            var x = testClass.GetLocalFiles2(testDictionary, @"E:\imageresizer\bigcontainer6", 2);

            IDbConnection dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));

            if (dbConnection.Query($"SELECT COUNT(tbl_name)  as 'amount' from sqlite_master where tbl_name = '{Environment.GetEnvironmentVariable("SQLiteBaseTableName") + "zobaczmy2"}'").FirstOrDefault().amount == 0)
                dbConnection.Execute($"CREATE TABLE \"{Environment.GetEnvironmentVariable("SQLiteBaseTableName") + "zobaczmy2"}\" (\n\t\"Id\"\tINTEGER NOT NULL UNIQUE,\n\t\"ImageName\"\tTEXT NOT NULL UNIQUE,\n\t\"Width\"\tINTEGER NOT NULL,\n\t\"Height\"\tINTEGER NOT NULL,\n\t\"Size\"\tTEXT NOT NULL,\n\tPRIMARY KEY(\"Id\" AUTOINCREMENT)\n)");


            foreach (var imagePath in x)
            {
                using (var openImage = File.Open(imagePath.Key, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))//FileStream openImage= File.OpenRead(imagePath.Key))
                {                
                  
                    openImage.Position = 0;
                    Image<Rgba32> imageObjCreatedForGettingImageData = (Image<Rgba32>)Image.Load(openImage);
                    ImageData imageData = new ImageData()
                    {
                        ImageName = Path.GetFileName(imagePath.Key),
                        Width = imageObjCreatedForGettingImageData.Width,
                        Height = imageObjCreatedForGettingImageData.Height,
                        Size = openImage.Length.ToString()
                    };
                    dbConnection.Execute($"insert into {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + "zobaczmy2"} (imageName,width,height,size) values (@imageName,@width,@height,@size)", imageData);
                    
                }
            }
            */


            // IDbConnection dbConnection = dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));

            var watch = new Stopwatch();
            watch.Start();

            IDatabaseService databaseService = new DatabaseService(Environment.GetEnvironmentVariable("DatabaseConnectionString"),true);
            
            watch.Stop();         
        


            //dbImagesList = dbImagesList.Select(x => x.ToString());

            return new OkObjectResult(watch.ElapsedMilliseconds);
        }
    }
}
