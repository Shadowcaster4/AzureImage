using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
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
