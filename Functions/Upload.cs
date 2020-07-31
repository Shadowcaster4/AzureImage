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
                
              //var imageFromHttp = req.Form.Files.GetFile(req.Form.Files[0].Name).l;
               var container = req.Form["container"];
                


                if(req.Form.Files.Count==0 || container == string.Empty)
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
                               

                for (int i = 0; i < req.Form.Files.Count; i++)
                {
                    string imagePath = service.GetImagePathUpload(req.Form.Files[i].FileName);
                    service.UploadImage(req.Form.Files.GetFile(req.Form.Files[i].Name).OpenReadStream(), container, imagePath);
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
