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

namespace ImageResizer
{

    public static class Resize
    {
        //connection string
        private static  readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        //method for creating resized images
        private static void CreateImage(MemoryStream imageFromStorage, BlobContainerClient containerClient, string blobFullName, int width, int height,int padding, string text = "duzo duzo duzo")
        {

            //font & color variables
            var fo = SystemFonts.Find("Arial");
            var font = new Font(fo, 20, FontStyle.Regular);
            PointF myPointIs = new PointF((width - (12 * text.Length)), height - 20);
            Color Rgba = Color.FromRgba(0, 155, 0, 130);


            using (var output = new MemoryStream())
            {

                    Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromStorage.ToArray());
                     
                    //resize & watermark
                    image.Mutate(x => x
                        .Resize(width, height).DrawText(text, font, Rgba, myPointIs)
                    );
                    
                    //padding 
                    if(padding>0)
                    {
                        var tmpImage = new Image<Rgba32>(width + padding, height + padding, Color.FromRgb(0, 0, 0));
                        tmpImage.Mutate(x => x.DrawImage(image, new Point(padding / 2, padding / 2), 1f));
                        image = tmpImage;
                    }
                    
                    //saving image with correct encoder to stream
                    if (blobFullName.Substring(blobFullName.Length - 3) == "jpg")
                        image.Save(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                    if (blobFullName.Substring(blobFullName.Length - 3) == "png")
                        image.Save(output, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

                    //saving image to blobstorage
                    output.Position = 0;
                    if (padding > 0) 
                    {
                         containerClient.UploadBlob(width + "/" + height + "/" + padding + "/" + blobFullName, output);
                    }
                    else
                    {
                         containerClient.UploadBlob(width + "/" + height + "/" + blobFullName, output);
                    }
                    
                    image.Dispose();
                               
            }
        }
        //checks if query parameters are correct
        private static string CheckQuery(HttpRequest request, string fileName)
        {
            string error = "";
            if (!(fileName.EndsWith(".png") || fileName.EndsWith(".jpg")))
                error += " not supported image extension ";
            if ((string.IsNullOrEmpty(request.Query["width"]) || string.IsNullOrEmpty(request.Query["height"])))
                error += " size parameter error ";

            return error;
        }


        [FunctionName("Resize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Resize/{name}")] HttpRequest req,
            string name,
            ILogger log)
        {
            var resp = new HttpResponseMessage();
            try
            {
                //returns BadRequestMessage if query parameters are incorrect
                if (!string.IsNullOrEmpty(CheckQuery(req, name)))
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent(CheckQuery(req, name));
                    return resp;
                }

                int padding = string.IsNullOrEmpty(req.Query["padding"]) ? 0 : Int32.Parse(req.Query["padding"]);
                int width = Int32.Parse(req.Query["width"]);
                int height = Int32.Parse(req.Query["height"]);


                //returns BadREquestMessage if width/height/padding are out of range
                if (width > 2000 || width < 10 || height > 2000 || height < 10 || padding < 0 || padding > width / 2 + height / 2 ? true : false)
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("wrong parameter values");
                    return resp;                    
                }

                //blob service connection
                var thumbContainerName = name.Remove(name.Length - 4);
                var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);

                //checks if image exist in storage
                if (blobServiceClient.GetBlobContainerClient(thumbContainerName).Exists())
                {
                    //get image container
                    var blobContainterClient = blobServiceClient.GetBlobContainerClient(thumbContainerName);

                    string path = "";
                    if (padding > 0)
                    {
                        path = width + "/" + height + "/" + padding + "/" + name;
                    }
                    else
                    {
                        path = width + "/" + height + "/"  + name;
                    }
                     
                    //get image
                    var blobObj = blobContainterClient.GetBlobBaseClient(path);

                    //if requested image already exists then return image uri else create new reqested image
                    if (blobObj.Exists())
                    {
                        resp.StatusCode = HttpStatusCode.OK;
                        resp.Content = new StringContent(blobObj.Uri.ToString());
                        return resp;
                    
                    }
                    else if (blobContainterClient.GetBlobBaseClient(name).Exists())
                    {
                        var existingBlobObj = blobContainterClient.GetBlobBaseClient(name);
                        BlobDownloadInfo download = await existingBlobObj.DownloadAsync();
                        MemoryStream mystream = new MemoryStream();
                        download.Content.CopyTo(mystream);
                        CreateImage(mystream, blobContainterClient, name, width, height,padding,"watermark");
                    }
                }
                else
                {
                    resp.StatusCode = HttpStatusCode.BadRequest;
                    resp.Content = new StringContent("requested picture doesn't exists");
                    return resp;
                   
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
                resp.StatusCode = HttpStatusCode.BadRequest;
                resp.Content = new StringContent("Something went wrong" + e.Message);
                return resp;
               
            }

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent("Image resized succesfully");
            return resp;
            
            
        }
    }
}
