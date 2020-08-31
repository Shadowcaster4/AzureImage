using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImageResizer
{

    public static class Resize
    {

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        [FunctionName("Resize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Resize/{parameters}/{container}/{imagename}")] HttpRequest req,
            string parameters,
            string container,
            string imagename)
        {
           
            try
            {
                Log.Info("Resize");
                IImageService service = Utilities.Utilities.GetImageService();
                IContainerService containerService = new ContainerClass(container);

                var requestedParameters = new QueryParameterValues(parameters);
                
                if (service.CheckIfParametersAreInRange(requestedParameters.Width,requestedParameters.Height))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "invalid parameter values");
                }

                if(!service.CheckIfFileIsSupported(imagename))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.BadRequest, "Not supported image type");
                }

                if (!service.CheckIfContainerExists(containerService))
                    throw new Exception("Container doesn't exists");

                if (!service.CheckIfImageExists(service.GetImagePathUpload(imagename),containerService))
                {
                    return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                        HttpStatusCode.NotFound, "Requested image doesn't exists");
                }

                //check if watermark  exist if not watermark presence parameter will be set to false
                if (!service.CheckIfImageExists(service.GetImagePathUpload("watermark.png"), containerService))
                    requestedParameters.SetWatermarkPresence(false);

                //check if hash from parameter is valid for requested picture
                else if (service.GetImageSecurityHash(container, imagename).Substring(0, 4) == requestedParameters.WatermarkString)
                    requestedParameters.SetWatermarkPresence(false);

                //set extension variable
                var imageExtension = service.GetImageExtension(imagename);
                var imagePath = service.GetImagePathResize(requestedParameters, imagename);

                //check if requested resolution is valid - original image resolution is >= requested resolution
                IDatabaseService databaseService = Utilities.Utilities.GetDatabaseService();
                var imageData = databaseService.GetImageData(imagename, containerService);
               
                var flagIsInOriginalImageRange = service.CheckIfImageRequestedImageResolutionIsInRange(
                    requestedParameters.Width, 
                    requestedParameters.Height, 
                    imageData);

                //if requested image resolution is out of range and requested image doesn't contain watermark then it will return image from original image stream
                if (!flagIsInOriginalImageRange)
                {
                    if(!requestedParameters.WatermarkPresence)
                    {
                        var tmpImg = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(imagename),containerService);
                        return Utilities.Utilities.GetImageHttpResponseMessage(tmpImg, imagename, imageExtension);
                    }
                    //if watermark will be used then new image will be created and saved in location created with "0,0" parameters
                    string newParametersValues = "0,0";
                    var oversizeImageParameters = new QueryParameterValues(newParametersValues);
                    requestedParameters = oversizeImageParameters;
                    imagePath = service.GetImagePathResize(requestedParameters, imagename);                   

                }              

                if (service.CheckIfImageExists(imagePath,containerService))
                {
                    var tmpImg = service.DownloadImageFromStorageToStream(imagePath,containerService);
                    return Utilities.Utilities.GetImageHttpResponseMessage(tmpImg, imagename, imageExtension);
                }

                //this part creates new resized image
                if(service.CheckIfImageExists(service.GetImagePathUpload(imagename),containerService))
                {  
                    //download base image from storage
                    var imageFromStorage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(imagename),containerService);
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

                    return Utilities.Utilities.GetImageHttpResponseMessage(mutatedImage, imagename, imageExtension);

                }
                
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.NotFound, "");

            }
            catch (Exception e)
            {
                Log.Error(e.Message);
               
                return Utilities.Utilities.GetHttpResponseMessage_ReturnsStatusCodeAndMessage(
                    HttpStatusCode.BadRequest, "Something gone wrong");

            }
            
        }
    }
}
