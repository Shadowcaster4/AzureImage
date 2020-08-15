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
using ImageResizer.Entities;

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
                if(req.Form.Files.Count==0 || req.Form["container"] == string.Empty)
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "invalid request data");
                }

                IImageService service = Utilities.Utilities.GetImageService();

                IContainerService container = new ContainerClass(req.Form["container"]);

                if (!service.CheckIfContainerNameIsValid(container))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "invalid container name");
                }

                for(int i=0;i<req.Form.Files.Count;i++)
                {
                    if (!service.ChceckIfFileIsSupported(req.Form.Files[i].FileName))
                    {
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.BadRequest, "invalid image format");
                    }

                    if (service.GetUploadImageSecurityKey(container.GetContainerName(), req.Form.Files[i].FileName, req.Form.Files[i].Length.ToString()) != req.Form.Files[i].Name)
                    {
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.Forbidden, "");
                      
                    }                    
                }

                List<string> NotUploadedFiles = new List<string>();

                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();
                
                    for (int i = 0; i < req.Form.Files.Count; i++)
                    {
                        string imagePath = service.GetImagePathUpload(req.Form.Files[i].FileName);
                        if (!service.UploadImage(
                            req.Form.Files.GetFile(req.Form.Files[i].Name).OpenReadStream(),
                            container,
                            imagePath,
                            databaseService))
                            NotUploadedFiles.Add(req.Form.Files[i].FileName);
                    }                
                 
                if(NotUploadedFiles.Any())
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.MultiStatus, JsonConvert.SerializeObject(value: NotUploadedFiles));
                }

                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.Created, "Uploaded successfully");

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.InternalServerError, "Something went wrong");
            }
            

        }
    }
}
