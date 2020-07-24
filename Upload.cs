using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;
using System.Web.Http;
using ImageResizer.Services;
using System.Linq;
using System.Net.Http;
using System.Net;
using ImageResizer.Services.Interfaces;

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
                
                var imageFromHttp = req.Form.Files.GetFile(req.Form.Files[0].Name);
                var container = req.Form["container"];
                
                if(imageFromHttp==null || container == string.Empty)
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("invalid request data");
                    return resp;
                }

                IImageService service = new ImageService();

                if(!service.CheckIfContainerNameIsValid(container))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("invalid container name");
                    return resp;
                }

                if(!service.ChceckIfFileIsSupported(imageFromHttp.FileName))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("invalid image format");
                    return resp;
                }

                string imagePath = service.GetImagePathUpload(imageFromHttp.FileName);
                service.UploadImage(imageFromHttp.OpenReadStream(), container, imagePath);
                resp.StatusCode = HttpStatusCode.Created;
                resp.Content = new StringContent("Image uploaded successfully");
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
