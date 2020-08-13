using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
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
            bool flagIsInOryginalImageRange = false;

            try
            {
                IImageService service =
                    Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));
                IContainerService containerService = new ContainerClass(clientHash);

                var requestedParameters = new QueryParameterValues(parameters);
                
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

                if (!service.CheckIfContainerExists(clientHash))
                    throw new Exception("Problem with container doesn't exists");

                if (!service.CheckIfImageExists(service.GetImagePathUpload(image),clientHash))
                {
                    resp.StatusCode = HttpStatusCode.NotFound;
                    resp.Content = new StringContent("Requested image doesn't exists");
                    return resp;
                }

                //checks if watermark image exist if not watermark presence parameter is set to false
                if (!service.CheckIfImageExists(service.GetImagePathUpload("watermark.png"), clientHash))
                    requestedParameters.SetWatermarkPresence(false);
                //checks if hash from parameter is valid for requested picture
                else if (service.GetImageSecurityHash(clientHash, image).Substring(0, 4) == requestedParameters.WatermarkString)
                    requestedParameters.SetWatermarkPresence(false);

                //sets image extension variable
                var imageExtension = service.GetImageExtension(image);
                var imagePath = service.GetImagePathResize(requestedParameters, image);

                //checks if requested resolution is valid - oryginal image resolution is >= requested resolution
                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService(null);
                var imageData = databaseService.GetImageData(image, clientHash);
                               
                    flagIsInOryginalImageRange = 
                        service.CheckIfImageRequestedImageResolutionIsInRange(
                            image, 
                            requestedParameters.Width, 
                            requestedParameters.Height, 
                            imageData);

                    //if requested image resolution is out of range and requested image doesnt contain watermark then it will return image from oryginal image stream
                if (!(flagIsInOryginalImageRange))
                {
                    if(!requestedParameters.WatermarkPresence)
                    {
                        var tmpImg = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image),containerService);
                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                        response.Content = new ByteArrayContent(tmpImg.GetBuffer());
                        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                        {
                            FileName = image
                        };
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + imageExtension);
                        response.Headers.CacheControl = new CacheControlHeaderValue()
                        {
                            Public = true,
                            MaxAge = new TimeSpan(14, 0, 0, 0)
                        };
                        return response;
                    }
                    string newParametersValues = "0,0";
                    var oversizeImageParameters = new QueryParameterValues(newParametersValues);
                    requestedParameters = oversizeImageParameters;
                    imagePath = service.GetImagePathResize(requestedParameters, image);                   

                }              

                if (service.CheckIfImageExists(imagePath,containerService.GetContainerName()))
                {
                    var tmpImg = service.DownloadImageFromStorageToStream(imagePath);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(tmpImg.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/"+imageExtension);
                    response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(14, 0, 0, 0)
                    };
                    return response;
                }

                if(service.CheckIfImageExists(service.GetImagePathUpload(image)))
                {                    
                    var imageFromStorage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image));
                    var mutadedImage = new MemoryStream();
                    
                    mutadedImage = service.MutateImage(imageFromStorage, requestedParameters.Width, requestedParameters.Height, requestedParameters.Padding,imageExtension,requestedParameters.WatermarkPresence);
                    service.SaveImage(mutadedImage, imagePath);

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(mutadedImage.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + imageExtension);
                    response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(14, 0, 0, 0)
                    };
                    return response;

                }

                resp.StatusCode = HttpStatusCode.NotFound;
                return resp;

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                resp.StatusCode = HttpStatusCode.BadRequest;
                resp.Content = new StringContent("Something went wrong" );
                return resp;
               
            }
            
        }
    }
}
