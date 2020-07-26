using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageResizer
{

    public static class Resize
    {               
   
        [FunctionName("Resize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Resize/{parameters}/{clientHash}/{image}")] HttpRequest req,
            string parameters,
            string clientHash,
            string image,
            ILogger log)
        {
            var resp = new HttpResponseMessage();
            try
            {
                IImageService service =new ImageService();                

                var requestedParameters = new QueryParameterValues(parameters);


                //returns BadREquestMessage if width/heightare out of range
                if (service.CheckIfParametersAreInRange(requestedParameters.Width,requestedParameters.Height))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("wrong parameter values");
                    return resp;                    
                }

                if(!service.ChceckIfFileIsSupported(image))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("Not supported image type");
                    return resp;
                }

                if (!service.SetServiceContainer(clientHash))
                    throw new Exception("Problem with container invalid name/doesnt exists");

                var imagePath = service.GetImagePathResize(parameters, image);
                var imageExtension = service.GetImageExtension(image);

                if (service.CheckIfImageExists(imagePath))
                {
                    var tmpImg = service.DownloadImageFromStorageToStream(imagePath);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(tmpImg.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/"+imageExtension);
                    return response;
                }

                if(service.CheckIfImageExists(service.GetImagePathUpload(image)))
                {
                    var imageFromStorage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image));
                    var mutadedImage = service.MutateImage(imageFromStorage, requestedParameters.Width, requestedParameters.Height, requestedParameters.Padding,imageExtension);
                    service.SaveImage(mutadedImage, imagePath);

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(mutadedImage.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + imageExtension);
                    return response;

                }
                
                
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                resp.StatusCode = HttpStatusCode.BadRequest;
                resp.Content = new StringContent("Something went wrong" );
                return resp;
               
            }

            resp.StatusCode = HttpStatusCode.NotFound;
            resp.Content = new StringContent("Requested image doesnt exists");
            return resp;
            
            
        }
    }
}
