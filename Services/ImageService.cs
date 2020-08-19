using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Dapper;
using ImageResizer.Entities;
using ImageResizer.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ImageResizer.Database;
using ImageResizer.Functions;
using System.Configuration;

namespace ImageResizer.Services
{
    public class ImageService : BaseService, IImageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        //private BlobContainerClient blobContainerClient;
        //private BlobBaseClient blobBaseClient;
        public ImageService(string applicationConnectionString) : base(applicationConnectionString)
        {
            _blobServiceClient = new BlobServiceClient(applicationConnectionString);            
        }

        public ImageService() : base()
        {
            _blobServiceClient = new BlobServiceClient(base._applicationConnectionString);            
        }

        private BlobContainerClient GetServiceContainer(IContainerService container)
        {
            if (!CheckIfContainerNameIsValid(container))
                throw new Exception("Invalid container name");

            if (CheckIfContainerExists(container))
            {
                return _blobServiceClient.GetBlobContainerClient(container.GetContainerName());
            }
            throw new Exception("container doesnt exists");
        }

        #region Containers Methods
        public bool CheckIfContainerExists(IContainerService container)
        {
            if (_blobServiceClient.GetBlobContainerClient(container.GetContainerName()).Exists())
            {
                return true;
            }
            return false;
        }
      
        public List<string> GetBlobContainers()
        {
            return _blobServiceClient.GetBlobContainers().Select(x=>x.Name).Where(x => !x.Contains("azure-webjobs")).ToList();            
        }

        public bool DeleteClientContainer(IContainerService container)
        {
            if (CheckIfContainerExists(container))
            {
                _blobServiceClient.DeleteBlobContainer(container.GetContainerName());
                return true;
            }
            return false;
        }

        public bool CreateClientContainer(IContainerService clientContainer)
        {
            if (CheckIfContainerExists(clientContainer))
                return false;
            _blobServiceClient.CreateBlobContainer(clientContainer.GetContainerName(), PublicAccessType.Blob);
            return true;
        }

        #endregion

        #region Image Blobs Methods
        public bool CheckIfImageExists(string imagePath, IContainerService container)
        {
            return GetServiceContainer(container).GetBlobBaseClient(imagePath).Exists();
        }

        public BlobBaseClient GetBlobImage(string imagePath, IContainerService container)
        {
            return GetServiceContainer(container).GetBlobBaseClient(imagePath);
            //return new BlobBaseClient(_applicationConnectionString,container.GetContainerName(),imagePath);
        }
    
        public Azure.Pageable<BlobItem> GetImagesFromContainer(IContainerService clientContainer)
        {
            try
            {
                if (!CheckIfContainerExists(clientContainer))
                    throw new Exception("Blob container is not set");
                return GetServiceContainer(clientContainer).GetBlobs();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public Dictionary<string, long> GetImagesDictionaryPathAndSize(IContainerService container)
        {
            var blobs = GetImagesFromContainer(container);

            Dictionary<string, long> imagesDictionary = blobs.ToDictionary(b => b.Name, b => b.Properties.ContentLength ?? 0);
            return imagesDictionary;
        }        

        public bool DeleteSingleCacheImage(string cacheImagePath, IContainerService container)
        {
            if (CheckIfImageExists(cacheImagePath,container))
            {
                GetBlobImage(cacheImagePath, container).Delete();
                return true;
            }
            return false;
        }

        public bool DeleteImageDirectory(string baseImageName, IContainerService container)
        {
            var containerObjects = GetImagesFromContainer(container);
            bool flag = false;
            foreach (BlobItem blobItem in containerObjects)
            {
                if (blobItem.Name.Contains("/" + baseImageName.Replace(".","") + "/"))
                {
                    GetServiceContainer(container).DeleteBlobIfExists(blobItem.Name);
                    flag = true;
                }                   
            }
            return flag;
        }

        public bool DeleteLetterDirectory(string fileName, IContainerService container)
        {
            var containerObjects = GetImagesFromContainer(container);
            bool flag = false;
            foreach (BlobItem blobItem in containerObjects)
            {
                if (blobItem.Name.StartsWith(fileName[0] + "/"))
                {
                    GetServiceContainer(container).DeleteBlobIfExists(blobItem.Name);
                    flag = true;
                }
            }
            return flag;
        }

        public ImageData GetImageProperties(Stream imageStream, string imageName, string container)
        {
            imageStream.Position = 0;
            Size imageSize = GetFileResolution.GetDimensions(new BinaryReader(imageStream));

            return new ImageData()
            {
                ImageName = Path.GetFileName(imageName),
                ClientContainer = container,
                Width = imageSize.Width,
                Height = imageSize.Height,
                Size = imageStream.Length.ToString()
            };
        }

        public ImageData UploadImage(Stream imageStream, IContainerService container,  string imagePath)
        {
            if (!CheckIfContainerExists(container))
              CreateClientContainer(container);
              
            if (CheckIfImageExists(imagePath,container))
                return new ImageData();

            var imageData = GetImageProperties(imageStream, Path.GetFileName(imagePath), container.GetContainerName());
            
            imageStream.Position = 0;

            GetServiceContainer(container).UploadBlob(imagePath, imageStream);
            
            imageStream.Dispose();
            return imageData;
        }

        public MemoryStream DownloadHeadOfImageFromStorageToStream(string imagePath, IContainerService container)
        {
            throw new NotImplementedException();
        }

        public bool CheckIfImageRequestedImageResolutionIsInRange(int width, int height, ImageData imageData)
        {
            return width <= imageData.Width || height <= imageData.Height;
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

        public Dictionary<string, CloudFileInfo> GetBaseImagesDictionary(IContainerService container)
        {
            if (!CheckIfContainerExists(container))
                throw new Exception("Blob container is not set");
            var blobs = GetServiceContainer(container)
                .GetBlobs().Where(x=>x.Name.Count(element=>element=='/')==2);

            var baseImagesDictionary =new Dictionary<string, CloudFileInfo>();

            foreach(BlobItem image in blobs)
            {
                baseImagesDictionary.Add(Path.GetFileName(image.Name), new CloudFileInfo(image.Properties.ContentLength?? 0, image.Properties.CreatedOn.Value.UtcDateTime));
            }    
            return baseImagesDictionary;
        }

        public Dictionary<string, DateTime> GetCachedImagesDictionary(IContainerService container)
        {
            if (!CheckIfContainerExists(container))
                throw new Exception("Blob container is not set");
            var blobs = GetServiceContainer(container)
                .GetBlobs().Where(x => x.Name.Count(element => element == '/') > 2);
            var cachedImagesDictionary = new Dictionary<string, DateTime>();

            foreach (BlobItem image in blobs)
            {
                cachedImagesDictionary.Add(image.Name, image.Properties.CreatedOn.Value.DateTime);
            }
            
            return cachedImagesDictionary;
        }

        public bool RemoveOldCache(IContainerService container, int days)
        {
            var cachedImagesDictionary = GetCachedImagesDictionary(container);
            bool flag = true;

            foreach (var item in cachedImagesDictionary)
            {
                if (item.Value < DateTime.UtcNow.AddDays(days * -1))
                    if (!DeleteSingleCacheImage(item.Key, container))
                        flag = false;
            }

            return flag;
        }

        #endregion

        #region Resize Methods

        public MemoryStream DownloadImageFromStorageToStream(string imagePath, IContainerService container)
        {
            MemoryStream outputStream = new MemoryStream();
            GetBlobImage(imagePath,container).DownloadTo(outputStream);
            
            return outputStream;
        }

        public IImageEncoder GetImageEncoder(string fileFormat)
        {
            return fileFormat switch
            {
                "png" => new PngEncoder(),
                "jpg" => new JpegEncoder(),
                "jpeg" => new JpegEncoder(),
                "gif" => new GifEncoder(),
                _ => new JpegEncoder(),
            };
        }

      
        public MemoryStream MutateImage(MemoryStream imageFromStorage, IContainerService container, int width, int height, bool padding, string fileFormat, bool watermark)
        {
            Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromStorage.ToArray());       

          
            if (watermark)
            {            
                MemoryStream watermarkStream = DownloadImageFromStorageToStream(GetImagePathUpload("watermark.png"),container);                
                Image<Rgba32> watermarkImage = (Image<Rgba32>)Image.Load(watermarkStream.ToArray());
                watermarkImage.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(image.Width / 10, image.Height / 10)
                }));

                image.Mutate(x => x.DrawImage(watermarkImage, new Point(image.Width - image.Width / 10, image.Height - image.Height / 10), 0.7f));

            }

            if (width > 0 && height > 0)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(width, height)
                }));
            }

            IImageEncoder imageFormatEncoder = GetImageEncoder(fileFormat);

            if (padding)
            {
                if (fileFormat == "png")
                {
                    var whiteBackgroundForPngImage = new Image<Rgba32>(image.Width, image.Height, Color.FromRgb(255, 255, 255));
                    whiteBackgroundForPngImage.Mutate(x => x.DrawImage(image, new Point(0, 0), 1.0f));
                    image = whiteBackgroundForPngImage;
                }
                //var
                Image<Rgba32> imageContainer = new Image<Rgba32>(width, height, Color.FromRgb(255, 0, 0));
                if (image.Width < imageContainer.Width)
                    imageContainer.Mutate(x => x.DrawImage(image, new Point((imageContainer.Width / 2) - (image.Width / 2), 0), 1.0f));
                else if (image.Height < imageContainer.Height)
                    imageContainer.Mutate(x =>
                        x.DrawImage(image, new Point(0, (imageContainer.Height / 2) - (image.Height / 2)), 1.0f));
                else
                    imageContainer = image;
                image = imageContainer;
            }
            var output = new MemoryStream();
            image.Save(output, imageFormatEncoder);
            image.Dispose();
            return output;
        }

        public bool SaveImage(MemoryStream imageToSave, string imagePath, IContainerService container)
        {
            if (!CheckIfContainerExists(container))
                return false;
            imageToSave.Position = 0;
            GetServiceContainer(container).UploadBlob(imagePath, imageToSave);
            return true;
        }
        
        #endregion

        #region Validation Methods /Utilities
        public bool CheckIfContainerNameIsValid(IContainerService container)
        {
            return Regex.IsMatch(container.GetContainerName(), @"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$");          
        }

        public bool CheckIfFileIsSupported(string fileName)
        {
            return (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif"));        
        }

        public string GetImageExtension(string fileName)
        {
            return fileName.Substring(fileName.LastIndexOf(".") + 1) switch
            {
                "png" => "png",
                "jpg" => "jpeg",
                "jpeg" => "jpeg",
                "gif" => "gif",
                _ => "notsupported"

            };
           
        }

        public bool CheckIfParametersAreInRange(int width, int height)
        {
            return (width > 2000 || width < 10 || height > 2000 || height < 10);
        }

        private string HashMyString(string stringToHash)
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
