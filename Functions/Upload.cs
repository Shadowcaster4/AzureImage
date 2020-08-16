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
using ServiceStack;

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
                if(!req.Form.Files.Any() || req.Form["container"] == string.Empty)
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

                foreach (var imageFile in req.Form.Files)
                {
                    if (!service.CheckIfFileIsSupported(imageFile.FileName))
                    {
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.BadRequest, "invalid image format");
                    }

                    if (service.GetUploadImageSecurityKey(container.GetContainerName(), imageFile.FileName, imageFile.Length.ToString()) != imageFile.Name)
                    {
                        return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                            HttpStatusCode.Forbidden, "");
                      
                    }
                }

                List<string> notUploadedFiles = new List<string>();
                List<ImageData> filesToUpload = new List<ImageData>();

                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();
             
                foreach (var imageToUpload in req.Form.Files)
                {
                    string imagePath = service.GetImagePathUpload(imageToUpload.FileName);
                    ImageData uploadResult = service.UploadImage(
                        req.Form.Files.GetFile(imageToUpload.Name).OpenReadStream(),
                        container,
                        imagePath);

                    if (uploadResult.ImageName.IsNullOrEmpty())
                        notUploadedFiles.Add(imageToUpload.FileName);
                    else
                    {
                        filesToUpload.Add(uploadResult);
                    }
                }
                
                if(filesToUpload.Any())
                    databaseService.SaveImagesData(filesToUpload);

                if (notUploadedFiles.Any())
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.MultiStatus, JsonConvert.SerializeObject(value: notUploadedFiles));
                

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
