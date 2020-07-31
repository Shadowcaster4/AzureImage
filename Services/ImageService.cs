using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ImageResizer.Entities;
using ImageResizer.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            bool flag = false;
            foreach (BlobItem blobItem in containerObjects)
            {
                if (blobItem.Name.Contains("/" + directoryName.Replace(".","") + "/"))
                {
                    blobContainerClient.DeleteBlobIfExists(blobItem.Name);
                    flag = true;
                }                   
            }
            return flag;
        }

        public bool DeleteLetterDirectory(string fileName)
        {
            var containerObjects = GetImagesFromContainer();
            bool flag = false;
            foreach (BlobItem blobItem in containerObjects)
            {
                if (blobItem.Name.StartsWith(fileName[0] + "/"))
                {
                    blobContainerClient.DeleteBlobIfExists(blobItem.Name);
                    flag = true;
                }
            }
            return flag;
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

        public string GetImagePathResize(QueryParameterValues parameters, string fileName)
        {            
            if(parameters.WatermarkPresence)
            return fileName[0] + "/" + fileName.Replace(".", "") + "/" + parameters.Width + "-" + parameters.Height + "-" + parameters.Padding + "-" + "watermark" + "/" + fileName;

            return fileName[0] + "/" + fileName.Replace(".", "") + "/" + parameters.Width + "-" + parameters.Height + "-" + parameters.Padding + "/" + fileName;
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

        public IImageEncoder GetImageEncoder(string fileFormat)
        {
            switch (fileFormat)
            {
                case "png":
                    return new PngEncoder();
                case "jpg":
                    return new JpegEncoder();
                case "jpeg":
                    return new JpegEncoder();
                default:
                    return new JpegEncoder();
            }
        }

        public MemoryStream MutateImage(MemoryStream imageFromStorage, int width, int heigth, bool padding,string fileFormat,bool watermark)
        {
            Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromStorage.ToArray());

            if(fileFormat=="png")
            {
                var whiteBackgroundForPngImage = new Image<Rgba32>(image.Width, image.Height, Color.FromRgb(255, 255, 255));
                whiteBackgroundForPngImage.Mutate(x => x.DrawImage(image, new Point(0, 0), 1.0f));
                image = whiteBackgroundForPngImage;
                
            }

            if(watermark)
            {
                MemoryStream watermarkStream = DownloadImageFromStorageToStream(GetImagePathUpload("watermark.png"));
                Image<Rgba32> watermarkImage = (Image<Rgba32>)Image.Load(watermarkStream.ToArray());
                watermarkImage.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(image.Width/10, image.Height/10)
                }));

                image.Mutate(x => x.DrawImage(watermarkImage, new Point(image.Width-image.Width/10 , image.Height - image.Height / 10), 0.7f));

            }

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width, heigth)
            }));

            IImageEncoder imageFormatEncoder = GetImageEncoder(fileFormat);

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
            image.Save(output, imageFormatEncoder);
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

        #region Validation Methods /Utilities
        public bool CheckIfContainerNameIsValid(string containerName)
        {
            if (!Regex.IsMatch(containerName, @"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$"))
                return false;
            return true;
        }

        public bool ChceckIfFileIsSupported(string fileName)
        {
            if (!(fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg")))
                return false;
            return true;
        }

        public string GetImageExtension(string fileName)
        {
            if (fileName.EndsWith(".png"))
                return "png";
            if (fileName.EndsWith(".jpg"))
                return "jpeg";
            if (fileName.EndsWith(".jpeg"))
                return "jpeg";

            return "not-supported";
            
        }

        public bool CheckIfParametersAreInRange(int width, int height)
        {
            return (width > 2000 || width < 10 || height > 2000 || height < 10 ? true : false);
        }
        public string HashMyString(string stringToHash)
        {
            var data = Encoding.ASCII.GetBytes(stringToHash);
            var hashData = new SHA1Managed().ComputeHash(data);
            var myHash = string.Empty;
            foreach (var b in hashData)
            {
                myHash += b.ToString("X2");
            }
            return myHash;
        }

        public string GetImageSecurityHash(string container, string imageName)
        {
            return HashMyString(HashMyString(container + imageName));
        }

        public string GetUploadImageSecurityKey(string container, string imageName,string imageSize)
        {
            return HashMyString(container + imageName + imageSize);
        }

        

        #endregion

    }
}
