using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using Azure.Storage.Blobs.Models;
using System.Web.Http;
using System.Net.Http;
using System.Net;
using System.Reflection.Metadata;
using ImageResizer.Services;
using ImageResizer.Models;
using System.Net.Http.Headers;

namespace ImageResizer
{

    public static class Resize
    {
        //connection string
        private static  readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
   
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
                var service =new ImageService(BLOB_STORAGE_CONNECTION_STRING);
                
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

                if(service.CheckIfImageExists(imagePath))
                {
                    var tmpImg = service.DownloadImageFromStorageToStream(imagePath);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(tmpImg.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    return response;
                }

                if(service.CheckIfImageExists(service.GetImagePathUpload(image)))
                {
                    var imageFromStorage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image));
                    var mutadedImage = service.MutateImage(imageFromStorage, requestedParameters.Width, requestedParameters.Height, requestedParameters.Padding);
                    service.SaveImage(mutadedImage, imagePath);

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(mutadedImage.GetBuffer());
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                    {
                        FileName = image
                    };
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
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
