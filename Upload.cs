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

namespace ImageResizer
{
    public static class Upload
    {

        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");


        [FunctionName("Upload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Upload")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a upload request.");
                var imageFromHttp = req.Form.Files.GetFile(req.Form.Files[0].Name);

                var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);
                var containers = blobServiceClient.GetBlobContainers();
                bool flag = false;

                if (!Regex.IsMatch(imageFromHttp.FileName.Remove(imageFromHttp.FileName.Length - 4), @"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$"))
                    return new OkObjectResult("Invalid file name");

                foreach (BlobContainerItem blobContainer in containers)
                {
                    if (blobContainer.Name == imageFromHttp.FileName.Remove(imageFromHttp.FileName.Length - 4))
                    {
                        flag = true;
                    }
                }
                if (flag)
                {
                    return new BadRequestErrorMessageResult("Sorry but this name is already taken");
                }
                blobServiceClient.CreateBlobContainer(imageFromHttp.FileName.Remove(imageFromHttp.FileName.Length - 4), PublicAccessType.Blob);
                var blobContainterClient = blobServiceClient.GetBlobContainerClient(imageFromHttp.FileName.Remove(imageFromHttp.FileName.Length - 4));


                using (var output = new MemoryStream())
                {
                    using (Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromHttp.OpenReadStream()))
                    {
                        if (imageFromHttp.FileName.Substring(imageFromHttp.FileName.Length - 3) == "jpg")
                            image.Save(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                        if (imageFromHttp.FileName.Substring(imageFromHttp.FileName.Length - 3) == "png")
                            image.Save(output, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

                        output.Position = 0;
                        await blobContainterClient.UploadBlobAsync(imageFromHttp.FileName, output);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                return new BadRequestErrorMessageResult("Something went wrong");
            }

            return new OkObjectResult("Success");
        }
    }
}
