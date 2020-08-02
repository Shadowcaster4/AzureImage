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

namespace ImageResizer.Functions
{
    public class tmpObj
    {
        public int amount { get; set; }
        
    }
    public class Test
    {
        
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            using(IDbConnection cnn = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString")))
            {

                //  
                //  cnn.Open();
                var image = new ImageData()
                {
                    ContainerName = "testowySzef",
                    ImageName = "grucha.png",
                    Width = 444,
                    Height = 555,
                    Size = "55555"
                };
                // cnn.Execute("insert into UploadedImagesResolution (containerName,imageName,width,height,size) values (@containerName,@imageName,@width,@height,@size)", image);
                string baseTableName = "UploadedImagesResolution_";
                string tableName = baseTableName + "xyz";
                var x = cnn.Query<tmpObj>($"SELECT COUNT(tbl_name) as 'amount' from sqlite_master where tbl_name = 'UploadedImagesResolution'",new DynamicParameters()).FirstOrDefault();
               
                // var y = JsonConvert.DeserializeAnonymousType(x.FirstOrDefault(p=>p.amount), new { amount = "" });
                //cnn.Query<ImageData>("delete  from UploadedImagesResolution where imageName='Bez_tytulu.png' AND containerName='testowykontenerdousuniecia'", new DynamicParameters());
                //cnn.Execute($"CREATE TABLE 'UploadedImagesResolution2' ('Id' INTEGER NOT NULL UNIQUE,'ImageName' TEXT NOT NULL UNIQUE,'Width' INTEGER NOT NULL,'Height' INTEGER NOT NULL, 'Size'  TEXT NOT NULL, PRIMARY KEY('Id' AUTOINCREMENT))");
                // cnn.Execute($"CREATE TABLE \"{tableName}\" (\n\t\"Id\"\tINTEGER NOT NULL UNIQUE,\n\t\"ImageName\"\tTEXT NOT NULL UNIQUE,\n\t\"Width\"\tINTEGER NOT NULL,\n\t\"Height\"\tINTEGER NOT NULL,\n\t\"Size\"\tTEXT NOT NULL,\n\tPRIMARY KEY(\"Id\" AUTOINCREMENT)\n)");  // var y = cnn.Query<ImageData>("select * from sqlite_master", new DynamicParameters());;
                // var y = cnn.Query("select * from sqlite_master", new DynamicParameters());;
                // cnn.Execute("DROP TABLE UploadedImagesResolution2");

                // ImageData imagedata = x.FirstOrDefault();
                
                    return new OkObjectResult(x.amount);
            }

            return new OkObjectResult("xD");
        }
    }
}
