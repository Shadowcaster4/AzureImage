﻿using Azure;
using Azure.Storage.Blobs.Models;
using Dapper;
using ImageResizer.Entities;
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageResizer.Services.Interfaces
{
    public class ImageServiceLocally : BaseService, IImageService
    {

        private readonly DirectoryInfo containerServiceClient;
        private DirectoryInfo directoryContainerClient;
        private FileInfo fileBaseClient;
        
                
        public ImageServiceLocally() : base()
        {
          //  containerServiceClient = new DirectoryInfo(base._applicationConnectionString);
            containerServiceClient = new DirectoryInfo(@"C:\Users\Tanatos\source\repos\import");
           // directoryContainerClient = new DirectoryInfo(@"C:\Users\Tanatos\source\repos\import\import");
            
        }
        

        public bool CheckIfContainerExists(string containerName)
        {     
            if (Directory.Exists(containerServiceClient.FullName + "\\" + containerName))
            {
               return true;
            }
            return false;
            
        }

      

        public bool CheckIfImageExists(string imagePath)
        {
            if (File.Exists(directoryContainerClient.FullName+"\\"+imagePath))   
                return true;
            return false;
        }


        public bool CheckIfImageRequestedImageResolutionIsInRange(string userContainerName, string imageName, int width, int height, IDbConnection dbConnection)
        {
            ImageData imageData = dbConnection.Query<ImageData>($"select * from {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + userContainerName} where imageName='{imageName}' ", new DynamicParameters()).FirstOrDefault();
            return width <= imageData.Width || height <= imageData.Height;
        }



        public bool CreateUsersContainer(string clientContainerName)
        {
            if (CheckIfContainerExists(clientContainerName))
                return false;
            containerServiceClient.CreateSubdirectory(clientContainerName);
         
            return true;
        }

        public bool DeleteCachedImage(string imagePath)
        {
            if (SetImageObject(imagePath))
            {
                fileBaseClient.Delete();
                return true;
            }
            return false;
        }

        public bool DeleteClientContainer(string clientContainerName)
        {
            directoryContainerClient.Delete(true);
            return true;
        }

        public bool DeleteImageDirectory(string directoryName)
        {
            Directory.Delete(directoryContainerClient.FullName+"\\"+directoryName, true);     
            return true;
        }

        public bool DeleteLetterDirectory(string fileName, IDbConnection dbConnection)
        {
           
            bool flag = false;

            foreach (var letterDir in directoryContainerClient.GetDirectories())
            {
                foreach (var dir in letterDir.GetDirectories())
                {
                    foreach (var image in dir.GetFiles())
                    {
                        dbConnection.Execute($"DELETE FROM {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + image.Name}   where imageName='{image.Name}'");
                        flag = true;
                    }
                }
            }

            Directory.Delete(directoryContainerClient.FullName + "\\" + fileName[0]);

            return flag;
        }


        public Dictionary<string, CloudFileInfo> GetBaseImagesDictionary()
        {
            if (!directoryContainerClient.Exists)
                throw new Exception("Blob container is not set");
         
            
            var BaseImagesDictionary = new Dictionary<string, CloudFileInfo>();

            foreach (var letterDir in directoryContainerClient.GetDirectories())
            {
                foreach (var dir in letterDir.GetDirectories())
                {
                    foreach (var image in dir.GetFiles())
                    {
                        BaseImagesDictionary.Add(Path.GetFileName(image.Name), new CloudFileInfo(image.Length, image.CreationTime));
                    }
                }
            }


        
            return BaseImagesDictionary;
        }

        public List<string> GetBlobContainers()
        {
            return containerServiceClient.GetDirectories().Select(x=>x.Name).ToList();
        }

        public Dictionary<string, DateTimeOffset> GetCachedImagesDictionary()
        {
            
            throw new NotImplementedException();
        }

       
        public string Test(string fileName)
        {
            string y = "";
            var x = containerServiceClient.GetFileSystemInfos();
            foreach(FileSystemInfo fileSystemInfo in x)
            {

                y += fileSystemInfo.Name;
            }
            return y;
        }


        public string GetImagePathResize(QueryParameterValues parameters, string fileName)
        {
            if (parameters.WatermarkPresence)
                return fileName[0] + "\\" + fileName.Replace(".", "") + "\\" + parameters.Width + "-" + parameters.Height + "-" + parameters.Padding + "-" + "watermark" + "\\" + fileName;

            return fileName[0] + "\\" + fileName.Replace(".", "") + "\\" + parameters.Width + "-" + parameters.Height + "-" + parameters.Padding + "\\" + fileName;
        }

        public string GetImagePathUpload(string fileName)
        {
            return fileName[0] + "\\" + fileName.Replace(".", "") + "\\" + fileName;
            
        }

        public Dictionary<string, long> GetImagesDictionarySize()
        {
            if (!directoryContainerClient.Exists)
                throw new Exception("Blob container is not set");
           
            Dictionary<string, long> imagesDictionary = new Dictionary<string, long>();

            foreach (var letterDir in directoryContainerClient.GetDirectories())
            {
                foreach (var dir in letterDir.GetDirectories())
                {
                    foreach (var file in dir.GetFiles())
                    {
                        imagesDictionary.Add(file.Name, file.Length);
                    }
                }
            }

            return imagesDictionary;
        }

       

        public Pageable<BlobItem> GetImagesFromContainer()
        {
            throw new NotImplementedException();
        }





       

        public bool SetImageObject(string imagePath)
        {
            if (CheckIfImageExists(imagePath))
            {
                fileBaseClient = new FileInfo(directoryContainerClient.FullName + "\\" + imagePath);
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
                directoryContainerClient = new DirectoryInfo(containerServiceClient.FullName + "\\" + containerName);
                return true;
            }
            return false;

           
        }

        public bool UploadImage(Stream image, string userContainerName, string imagePath, IDbConnection dbConnection)
        {
            if (!SetServiceContainer(userContainerName))
            {
                CreateUsersContainer(userContainerName);
                SetServiceContainer(userContainerName);
            }

            if (CheckIfImageExists(imagePath))
                return false;

            Image<Rgba32> imageObjCreatedForGettingImageData = (Image<Rgba32>)Image.Load(image);
            ImageData imageData = new ImageData()
            {
                ImageName = imagePath.Substring(imagePath.LastIndexOf('\\') + 1),
                Width = imageObjCreatedForGettingImageData.Width,
                Height = imageObjCreatedForGettingImageData.Height,
                Size = image.Length.ToString()
            };

            image.Position = 0;

            string fullPath = directoryContainerClient.FullName + "\\" + imagePath;
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using (FileStream uploadImage = File.Create(fullPath))
            {
                MemoryStream tmpStream = new MemoryStream();
                image.CopyTo(tmpStream);
                tmpStream.WriteTo(uploadImage);
            }
            

            dbConnection.Execute($"insert into {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + userContainerName} (imageName,width,height,size) values (@imageName,@width,@height,@size)", imageData);

            imageObjCreatedForGettingImageData.Dispose();
            image.Dispose();
            return true;
        }

        #region Resize Methods
        public MemoryStream DownloadImageFromStorageToStream(string imagePath)
        {
            SetImageObject(imagePath);
            MemoryStream outputStream = new MemoryStream();
            fileBaseClient.Open(FileMode.Open).CopyTo(outputStream);                    
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
                
        public MemoryStream MutateImage(MemoryStream imageFromStorage, int width, int heigth, bool padding, string fileFormat, bool watermark)
        {
            Image<Rgba32> image = (Image<Rgba32>)Image.Load(imageFromStorage.ToArray());
           
            if (watermark)
            {
                MemoryStream watermarkStream = DownloadImageFromStorageToStream(GetImagePathUpload("watermark.png"));
                Image<Rgba32> watermarkImage = (Image<Rgba32>)Image.Load(watermarkStream.ToArray());
                watermarkImage.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(image.Width / 10, image.Height / 10)
                }));

                image.Mutate(x => x.DrawImage(watermarkImage, new Point(image.Width - image.Width / 10, image.Height - image.Height / 10), 0.7f));

            }

            if (width > 0 && heigth > 0)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(width, heigth)
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

                Image<Rgba32> imageContainer = new Image<Rgba32>(width, heigth, Color.FromRgb(255, 0, 0));
                if (image.Width < imageContainer.Width)
                    imageContainer.Mutate(x => x.DrawImage(image, new Point((imageContainer.Width / 2) - (image.Width / 2), 0), 1.0f));
                if (image.Height < imageContainer.Height)
                    imageContainer.Mutate(x => x.DrawImage(image, new Point(0, (imageContainer.Height / 2) - (image.Height / 2)), 1.0f));
                image = imageContainer;
            }
            var output = new MemoryStream();
            image.Save(output, imageFormatEncoder);
            image.Dispose();
            return output;
        }
        public bool SaveImage(MemoryStream imageToSave, string imagePath)
        {
            if (!directoryContainerClient.Exists)
                return false;
            imageToSave.Position = 0;
            string fullPath = directoryContainerClient.FullName + "\\" + imagePath;

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using FileStream saveResizedImage = File.Create(directoryContainerClient.FullName+"\\" + imagePath);
            {
                imageToSave.WriteTo(saveResizedImage);
            }
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
            if (!(fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif")))
                return false;
            return true;
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

        public string GetUploadImageSecurityKey(string container, string imageName, string imageSize)
        {
            return HashMyString(container + imageName + imageSize);
        }

       
        #endregion
    }
}
