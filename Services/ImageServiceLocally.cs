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
using ImageResizer.Database;
using ImageResizer.Functions;
using ServiceStack;

namespace ImageResizer.Services.Interfaces
{
    public class ImageServiceLocally : BaseService, IImageService
    {

        private readonly DirectoryInfo _serviceClient;

        public ImageServiceLocally() : base()
        {
            _serviceClient = new DirectoryInfo(_applicationConnectionString);
        }

        public ImageServiceLocally(string applicationConnectionString) : base(applicationConnectionString)
        {
            _serviceClient = new DirectoryInfo(applicationConnectionString);
        }

        #region Containers Methods
        public bool CheckIfContainerExists(IContainerService container)
        {
            return !container.GetContainerName().IsNullOrEmpty() && Directory.Exists(Path.Combine(_serviceClient.FullName,container.GetContainerName()));
        }
        
        private string GetFullFilePath(IContainerService container, string filePath)
        {
            return Path.Combine(_serviceClient.FullName,container.GetContainerName(), filePath);
        }

     
        private DirectoryInfo  GetServiceContainer(IContainerService container)
        {
            if (!CheckIfContainerNameIsValid(container))
                throw new Exception("Invalid container name");

            if (CheckIfContainerExists(container))
                return new DirectoryInfo(Path.Combine(_serviceClient.FullName,container.GetContainerName()));
            
            throw new Exception("container doesn't exists");
        }

        public List<string> GetBlobContainers()
        {
            return _serviceClient.GetDirectories()
                .Select(x => x.Name)
                .Where(x=>CheckIfContainerNameIsValid(
                    new ContainerClass(x))
                ).ToList();
        }


        public bool DeleteClientContainer(IContainerService clientContainer)
        {
            try
            {
                 GetServiceContainer(clientContainer).Delete(true);
                 return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public bool CreateClientContainer(IContainerService clientContainer)
        {
            if (CheckIfContainerExists(clientContainer))
                return false;
            _serviceClient.CreateSubdirectory(clientContainer.GetContainerName());
            return true;
        }
        #endregion
        #region Image Blobs Methods

        public bool CheckIfImageExists(string imagePath, IContainerService container)
        {
            return File.Exists(
                GetFullFilePath(
                    container,
                    imagePath
                    ));
        }

       
        public Dictionary<string, long> GetImagesDictionaryPathAndSize(IContainerService container)
        {
            var containerClient = GetServiceContainer(container);

            var containerFiles = containerClient.GetFiles("*", SearchOption.AllDirectories)
                .Where(x=>CheckIfFileIsSupported(x.Name));

            return containerFiles.ToDictionary(file => file.FullName, file => file.Length);
        }

        public bool DeleteSingleCacheImage(string cacheImagePath,IContainerService container)
        {
            if (CheckIfImageExists(cacheImagePath,container))
            {
                string path = Path.GetDirectoryName(GetFullFilePath(container,cacheImagePath));
                Directory.Delete(Path.GetFullPath(path), true);
                return true;
            }
            return false;
        }

        public bool DeleteImageDirectory(string baseImageName,IContainerService container)
        {
            var fileToDelete = new FileInfo(GetFullFilePath(container, GetImagePathUpload(baseImageName)));

            if (fileToDelete.Exists)
            {
                string directoryPath =Path.Combine(fileToDelete.DirectoryName,fileToDelete.Name.Replace(".",""));
                fileToDelete.Delete();
                if(Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
                return true;
            }
            return false;
        }

        public bool DeleteLetterDirectory(string fileName, IContainerService container)
        {
           try
           {
               Directory.Delete(Path.Combine(_serviceClient.FullName,container.GetContainerName(),fileName[0].ToString()), true);
               return true;
           }
           catch (Exception e)
           {
               return false;
           }
           
        }
        

        public ImageData GetImageProperties(Stream imageStream,string imageName, string container)
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

        public ImageData UploadImage(Stream imageStream, IContainerService container, string imagePath)
        {
           
            if (!CheckIfContainerExists(container))
                CreateClientContainer(container);
         

            if (CheckIfImageExists(imagePath,container))
                return new ImageData();

            var imageData = GetImageProperties(imageStream, Path.GetFileName(imagePath), container.GetContainerName());
            
            string fullPath = GetFullFilePath(container,imagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            
            using (FileStream uploadImage = File.Create(fullPath))
            {
                imageStream.Position = 0;
                imageStream.WriteTo(uploadImage);
                uploadImage.Dispose();
            }
     
            imageStream.Dispose();
            return imageData;
        }

        public bool CheckIfImageRequestedImageResolutionIsInRange(int width, int height,ImageData imageData)
        {
               return width <= imageData.Width || height <= imageData.Height;
        }

        public string GetImagePathResize(QueryParameterValues parameters, string fileName)
        {
            if (parameters.WatermarkPresence)
                return Path.Combine(fileName[0].ToString(),
                fileName.Replace(".", ""),
                parameters.Width + "-" + parameters.Height + "-" + parameters.Padding + "-" + "watermark",
                fileName);

            return Path.Combine(fileName[0].ToString(),
                fileName.Replace(".", ""),
                parameters.Width + "-" + parameters.Height + "-" + parameters.Padding ,
                fileName);
        }

        public string GetImagePathUpload(string fileName)
        {
            return Path.Combine(fileName[0].ToString() , fileName);
        }

        public Dictionary<string, CloudFileInfo> GetBaseImagesDictionary(IContainerService container)
        {
            var containerClient = GetServiceContainer(container);

            Dictionary<string, LocalFileInfo> myBaseImagesDictionary = new Dictionary<string, LocalFileInfo>();

            GetLocalFiles(myBaseImagesDictionary, containerClient.FullName, 1);
            return myBaseImagesDictionary.ToDictionary(x => Path.GetFileName(x.Key), x => new CloudFileInfo(x.Value.Size, x.Value.Date));
        }

        public Dictionary<string, DateTime> GetCacheImagesDictionary(IContainerService container)
        {
            Dictionary<string, LocalFileInfo> cachedImages = new Dictionary<string, LocalFileInfo>();
            var containerClient = GetServiceContainer(container);
            GetLocalFiles(cachedImages, containerClient.FullName, 3);

            return cachedImages.ToDictionary(x =>x.Key, x => x.Value.Date);
        }


        private Dictionary<string, LocalFileInfo> GetLocalFiles(Dictionary<string, LocalFileInfo> myFiles, string startLocation, int depth)
        {
            string[] subDirs = Directory.GetDirectories(startLocation);

            if (depth == 0)
            {
                string[] files = Directory.GetFiles(startLocation, "*.*");
          
                foreach (string file in files.Where(CheckIfFileIsSupported))
                {
                    FileInfo tmpFile = new FileInfo(Path.GetFullPath(file));
                    myFiles.Add(file, new LocalFileInfo(tmpFile.Name, tmpFile.Length, tmpFile.CreationTime));
                }
                return myFiles;
            }

            foreach (string dir in subDirs)
            {
                GetLocalFiles(myFiles, dir, depth - 1);
            }
            return myFiles;
        }


        public void RemoveOldCache(IContainerService container, int days)
        {
            
            Dictionary<string, DateTime> cacheImagesDictionary = GetCacheImagesDictionary(container);
            DateTime removeDay = DateTime.UtcNow.AddDays(days * -1);

            foreach (var item in cacheImagesDictionary)
            {
                if (item.Value < removeDay)
                {
                    FileInfo tmpFileInfo = new FileInfo(item.Key);
                    tmpFileInfo.Directory.Delete(true);
                    if (tmpFileInfo.Directory.Parent.GetDirectories().IsEmpty())
                        tmpFileInfo.Directory.Parent.Delete(true);
                }
            }

           
        }

        #endregion
        #region Resize Methods
        public MemoryStream DownloadImageFromStorageToStream(string imagePath,IContainerService container)
        {
            MemoryStream outputStream = new MemoryStream();
            using(var fs = File.Open(GetFullFilePath(container,imagePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.CopyTo(outputStream);
                fs.Dispose();
            }                   
            return outputStream;
        }

        public MemoryStream DownloadHeadOfImageFromStorageToStream(string imagePath, IContainerService container)
        {
            MemoryStream outputStream = new MemoryStream();
            using (var fs = File.Open(GetFullFilePath(container, imagePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
               
                if (Path.GetFileName(imagePath).EndsWith(".gif") || Path.GetFileName(imagePath).EndsWith(".png"))
                {
                    byte[] bytes = new byte[24];
                    fs.Read(bytes, 0, 24);
                    outputStream = new MemoryStream(bytes);
                }
                else
                {
                    byte[] bytes = new byte[200];
                    fs.Read(bytes, 0, 200);
                    outputStream = new MemoryStream(bytes);
                }

                fs.Dispose();
            }
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
                
        public MemoryStream MutateImage(MemoryStream imageFromStorage,IContainerService container , int width, int height, bool padding, string fileFormat, bool watermark)
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
                watermarkStream.Dispose();
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
        public bool SaveImage(MemoryStream imageToSave, string imagePath,IContainerService container)
        {

            var containerClient = GetServiceContainer(container);
           
            imageToSave.Position = 0;
            string fullPath = Path.Combine( containerClient.FullName, imagePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
          
            using FileStream saveResizedImage = File.Create(fullPath);
            {
                imageToSave.WriteTo(saveResizedImage);       
            }
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
            return (width > 2000 || width < 10 || height > 2000 || height < 10 ? true : false);
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

        public string GetUploadImageSecurityKey(string container, string imageName, string imageSize)
        {
            return HashMyString(container + imageName + imageSize);
        }


        #endregion
        
    }
}
