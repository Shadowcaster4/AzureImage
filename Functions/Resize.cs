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
            try
            {
                IImageService service = Utilities.Utilities.GetImageService();
                IContainerService containerService = new ContainerClass(clientHash);

                var requestedParameters = new QueryParameterValues(parameters);
                
                if (service.CheckIfParametersAreInRange(requestedParameters.Width,requestedParameters.Height))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "invalid parameter values");
                }

                if(!service.CheckIfFileIsSupported(image))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "Not supported image type");
                }

                if (!service.CheckIfContainerExists(containerService))
                    throw new Exception("Problem with container doesn't exists");

                if (!service.CheckIfImageExists(service.GetImagePathUpload(image),containerService))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Requested image doesn't exists");
                }

                //checks if watermark image exist if not watermark presence parameter is set to false
                if (!service.CheckIfImageExists(service.GetImagePathUpload("watermark.png"), containerService))
                    requestedParameters.SetWatermarkPresence(false);

                //checks if hash from parameter is valid for requested picture
                else if (service.GetImageSecurityHash(clientHash, image).Substring(0, 4) == requestedParameters.WatermarkString)
                    requestedParameters.SetWatermarkPresence(false);

                //sets image extension variable
                var imageExtension = service.GetImageExtension(image);
                var imagePath = service.GetImagePathResize(requestedParameters, image);

                //checks if requested resolution is valid - oryginal image resolution is >= requested resolution
                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();
                var imageData = databaseService.GetImageData(image, containerService);
               
                var flagIsInOryginalImageRange = service.CheckIfImageRequestedImageResolutionIsInRange(
                    requestedParameters.Width, 
                    requestedParameters.Height, 
                    imageData);

                //if requested image resolution is out of range and requested image doesn't contain watermark then it will return image from original image stream
                if (!flagIsInOryginalImageRange)
                {
                    if(!requestedParameters.WatermarkPresence)
                    {
                        var tmpImg = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image),containerService);
                        return Utilities.Utilities.GetImageHttpResponseMessage(tmpImg, image, imageExtension);
                    }
                    //if watermark will be used then new image will be created and saved in location created from "0,0" parameters
                    string newParametersValues = "0,0";
                    var oversizeImageParameters = new QueryParameterValues(newParametersValues);
                    requestedParameters = oversizeImageParameters;
                    imagePath = service.GetImagePathResize(requestedParameters, image);                   

                }              

                if (service.CheckIfImageExists(imagePath,containerService))
                {
                    var tmpImg = service.DownloadImageFromStorageToStream(imagePath,containerService);
                    return Utilities.Utilities.GetImageHttpResponseMessage(tmpImg, image, imageExtension);
                }

                //this part creates new resized image
                if(service.CheckIfImageExists(service.GetImagePathUpload(image),containerService))
                {  
                    //download base image from storage
                    var imageFromStorage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(image),containerService);
                    //create new image with requested parameters
                    var mutatedImage = service.MutateImage(
                        imageFromStorage,
                        containerService,
                        requestedParameters.Width, 
                        requestedParameters.Height, 
                        requestedParameters.Padding,
                        imageExtension,
                        requestedParameters.WatermarkPresence);
                    //save created image
                    service.SaveImage(mutatedImage, imagePath,containerService);

                    return Utilities.Utilities.GetImageHttpResponseMessage(mutatedImage, image, imageExtension);

                }
                
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.NotFound, "");

            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
               
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.BadRequest, "Something went wrong");

            }
            
        }
    }
}
