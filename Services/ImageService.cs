using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ImageResizer.Models;
using ImageResizer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Azure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageResizer.Services
{
    public class ImageService : BaseService, IImageService
    {
        private readonly BlobServiceClient blobServiceClient;
        private BlobContainerClient blobContainerClient;
        private BlobBaseClient blobBaseClient;
        public ImageService(string applicationConnectionString) : base(applicationConnectionString)
        {
            blobServiceClient = new BlobServiceClient(applicationConnectionString);
        }

        public ImageService() : base()
        {
            blobServiceClient = new BlobServiceClient(base._applicationConnectionString);
        }

        #region Containers Methods
        public bool CheckIfContainerExists(string containerName)
        {
            if (blobServiceClient.GetBlobContainerClient(containerName).Exists())
            {
                return true;
            }
            return false;
        }
        public bool SetServiceContainer(string containerName)
        {
            if (!CheckIfContainerNameIsValid(containerName))
                return false;

            if (CheckIfContainerExists(containerName))
            {
                blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                return true;
            }
            return false;

        }
        public Azure.Pageable<BlobContainerItem> GetBlobContainers()
        {
            return blobServiceClient.GetBlobContainers();
        }

        public bool DeleteClientContainer(string clientContainerName)
        {
            if (CheckIfContainerExists(clientContainerName))
            {
                blobServiceClient.DeleteBlobContainer(clientContainerName);
                return true;
            }
            return false;
        }

        public bool CreateUsersContainer(string clientContainerName)
        {
            if (CheckIfContainerExists(clientContainerName))
                return false;
            blobServiceClient.CreateBlobContainer(clientContainerName, PublicAccessType.Blob);
            return true;
        }

        #endregion

        #region Image Blobs Methods
        public bool CheckIfImageExists(string imagePath)
        {
            if (blobContainerClient.GetBlobBaseClient(imagePath).Exists())
                return true;
            return false;
        }
        public bool SetImageObject(string imagePath)
        {
            if (CheckIfImageExists(imagePath))
            {
                blobBaseClient = blobContainerClient.GetBlobBaseClient(imagePath);
                return true;
            }
            return false;
        }
        public Azure.Pageable<BlobItem> GetImagesFromContainer()
        {
            try
            {
                if (!blobContainerClient.Exists())
                    throw new Exception("Blob container is not set");
                return blobContainerClient.GetBlobs();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public Dictionary<string, long> GetImagesDictionarySize()
        {
            if (!blobContainerClient.Exists())
                throw new Exception("Blob container is not set");
            var blobs = blobContainerClient.GetBlobs();

            Dictionary<string, long> imagesDictionary = blobs.ToDictionary(b => b.Name, b => b.Properties.ContentLength ?? 0);
            return imagesDictionary;
        }

        public Dictionary<string, DateTimeOffset> GetImagesDictionaryDate()
        {
            if (!blobContainerClient.Exists())
                throw new Exception("Blob container is not set");
            var blobs = blobContainerClient.GetBlobs();

            Dictionary<string, DateTimeOffset> imagesDictionary = blobs.ToDictionary(b => b.Name, b => b.Properties.CreatedOn ?? new DateTimeOffset(DateTime.UtcNow.AddDays(2)));
            // Dictionary<string, long> imagesDictionary = blobs.ToDictionary(b => b.Name.Substring(b.Name.LastIndexOf('/')+1), b => b.Properties.ContentLength ??0);
            return imagesDictionary;
        }

        

        public bool DeleteCachedImage(string imagePath)
        {
            if (SetImageObject(imagePath))
            {
                blobBaseClient.Delete();
                return true;
            }
            return false;
        }

        public bool DeleteImageDirectory(string directoryName)
        {
            var containerObjects = GetImagesFromContainer();
            foreach (BlobItem blobItem in containerObjects)
            {
                if (blobItem.Name.Contains("/" + directoryName + "/"))
                    blobContainerClient.DeleteBlobIfExists(blobItem.Name);
            }
            return true;
        }

        public bool UploadImage(Stream image, string usersContainerName, string imagePath)
        {
            if (!SetServiceContainer(usersContainerName))
            {
                CreateUsersContainer(usersContainerName);
                SetServiceContainer(usersContainerName);
            }
            image.Position = 0;
            blobContainerClient.UploadBlob(imagePath, image);
            return true;
        }

        public string GetImagePathResize(string parameters, string fileName)
        {
            var fileParameters = new QueryParameterValues(parameters);
            return fileName[0] + "/" + fileName.Replace(".", "") + "/" + fileParameters.Width + "-" + fileParameters.Height + "-" + fileParameters.Padding + "/" + fileName;
        }

        public string GetImagePathUpload(string fileName)
        {
            return fileName[0] + "/" + fileName.Replace(".", "") + "/" + fileName;
        }

        public Dictionary<string, CloudFileInfo> GetBaseImagesDictionary()
        {
            if (!blobContainerClient.Exists())
                throw new Exception("Blob container is not set");
            var blobs = blobContainerClient.GetBlobs().Where(x=>x.Name.Count(element=>element=='/')==2);
            var BaseImagesDictionary =new Dictionary<string, CloudFileInfo>();

            foreach(BlobItem image in blobs)
            {
                BaseImagesDictionary.Add(Path.GetFileName(image.Name), new CloudFileInfo(image.Properties.ContentLength?? 0, image.Properties.CreatedOn?? new DateTimeOffset(DateTime.UtcNow.AddDays(2))));
            }    
           
            return BaseImagesDictionary;
        }

        public Dictionary<string, DateTimeOffset> GetCachedImagesDictionary()
        {
            if (!blobContainerClient.Exists())
                throw new Exception("Blob container is not set");
            var blobs = blobContainerClient.GetBlobs().Where(x => x.Name.Count(element => element == '/') > 2);
            var CachedImagesDictionary = new Dictionary<string, DateTimeOffset>();

            foreach (BlobItem image in blobs)
            {
                CachedImagesDictionary.Add(image.Name, image.Properties.CreatedOn ?? new DateTimeOffset(DateTime.UtcNow.AddDays(2)));
            }

            return CachedImagesDictionary;
        }
        #endregion

        #region Resize Methods

        public MemoryStream DownloadImageFromStorageToStream(string imagePath)
        {
            SetImageObject(imagePath);
            BlobDownloadInfo downloadInfo = blobBaseClient.Download();
            MemoryStream outputStream = new MemoryStream();
            downloadInfo.Content.CopyTo(outputStream);
            return outputStream;
        }

        public MemoryStream MutateImage(MemoryStream imageFromStorage, int width, int heigth, bool padding)
        {
            Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromStorage.ToArray());
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width, heigth)
            }));

            if (padding)
            {
                var imageContainer = new Image<Rgba32>(width, heigth, Color.FromRgb(255, 0, 0));
                if (image.Width < imageContainer.Width)
                    imageContainer.Mutate(x => x.DrawImage(image, new Point((imageContainer.Width / 2) - (image.Width / 2), 0), 1.0f));
                if (image.Height < imageContainer.Height)
                    imageContainer.Mutate(x => x.DrawImage(image, new Point(0, (imageContainer.Height / 2) - (image.Height / 2)), 1.0f));
                image = imageContainer;
            }
            var output = new MemoryStream();
            image.Save(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            return output;
        }

        public bool SaveImage(MemoryStream imageToSave, string imagePath)
        {
            if (!blobContainerClient.Exists())
                return false;
            imageToSave.Position = 0;
            blobContainerClient.UploadBlob(imagePath, imageToSave);
            return true;
        }


        #endregion

        #region Validation Methods
        public bool CheckIfContainerNameIsValid(string containerName)
        {
            if (!Regex.IsMatch(containerName, @"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$"))
                return false;
            return true;
        }

        public bool ChceckIfFileIsSupported(string fileName)
        {
            if (!(fileName.EndsWith(".png") || fileName.EndsWith(".jpg")))
                return false;
            return true;
        }

        public bool CheckIfParametersAreInRange(int width, int height)
        {
            return (width > 2000 || width < 10 || height > 2000 || height < 10 ? true : false);
        }


        #endregion

    }
}
