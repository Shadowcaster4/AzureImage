using Dapper;
using ImageResizer.Database;
using ImageResizer.Functions;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageResizer
{
    public static class Upload
    {
             
        [FunctionName("Upload")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Upload")] HttpRequest req,
            ILogger log)
        {
            var resp = new HttpResponseMessage();
            try
            {  
                var container = req.Form["container"];

                if(req.Form.Files.Count==0 || container == string.Empty)
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("invalid request data");
                    return resp;
                }

                IImageService service =
                    Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

                if (!service.CheckIfContainerNameIsValid(container))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("invalid container name");
                    return resp;
                }

                for(int i=0;i<req.Form.Files.Count;i++)
                {
                    if (!service.ChceckIfFileIsSupported(req.Form.Files[i].FileName))
                    {
                        resp.StatusCode = HttpStatusCode.BadRequest;
                        resp.Content = new StringContent("invalid image format");
                        return resp;
                    }

                    if (service.GetUploadImageSecurityKey(container, req.Form.Files[i].FileName, req.Form.Files[i].Length.ToString()) != req.Form.Files[i].Name)
                    {
                        resp.StatusCode = HttpStatusCode.Forbidden;
                        return resp;
                    }                    
                }

                List<string> NotUploadedFiles = new List<string>();

                IDatabaseService databaseService = new DatabaseService("");
                
                //if container dbtable doesnt exists this will create it
                databaseService.dbConnection2.Execute($"CREATE TABLE if not exists '{(Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container)}' (Id INTEGER NOT NULL UNIQUE,ImageName TEXT NOT NULL UNIQUE,Width INTEGER NOT NULL,Height INTEGER NOT NULL,Size TEXT NOT NULL, PRIMARY KEY(Id AUTOINCREMENT))");

                    for (int i = 0; i < req.Form.Files.Count; i++)
                    {
                        string imagePath = service.GetImagePathUpload(req.Form.Files[i].FileName);
                        if (!service.UploadImage(req.Form.Files.GetFile(req.Form.Files[i].Name).OpenReadStream(), container, imagePath, databaseService.dbConnection2))
                            NotUploadedFiles.Add(req.Form.Files[i].FileName);
                    }
                
                 
                if(NotUploadedFiles.Count>0)
                {
                    resp.StatusCode = HttpStatusCode.MultiStatus;
                    resp.Content = new StringContent(JsonConvert.SerializeObject(value:NotUploadedFiles));
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return resp;
                }
                resp.StatusCode = HttpStatusCode.Created;
                resp.Content = new StringContent("Uploaded successfully");
                return resp;
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                resp.StatusCode = HttpStatusCode.InternalServerError;
                resp.Content = new StringContent("Something went wrong");
                return resp;
            }
            

        }
    }
}
