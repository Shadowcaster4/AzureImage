using ImageResizer.Entities;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageResizer.Services.Interfaces
{
    public interface IImageService
    {
        bool CheckIfFileIsSupported(string fileName);
        bool CheckIfContainerExists(IContainerService container);
        bool CheckIfContainerNameIsValid(IContainerService container);
        bool CheckIfImageExists(string imagePath, IContainerService container);
        bool CheckIfParametersAreInRange(int width, int height);
        bool CheckIfImageRequestedImageResolutionIsInRange(int width, int height, ImageData imageData);
        bool CreateClientContainer(IContainerService clientContainer);
        bool DeleteSingleCacheImage(string cacheImagePath, IContainerService container);
        bool DeleteClientContainer(IContainerService clientContainer);
        bool DeleteImageDirectory(string baseImageName, IContainerService container);
        bool DeleteLetterDirectory(string fileName, IContainerService container);
        MemoryStream DownloadImageFromStorageToStream(string imagePath, IContainerService container);
        List<string> GetBlobContainers();
        string GetImagePathResize(QueryParameterValues parameters, string fileName);
        string GetImagePathUpload(string fileName);
        string GetImageExtension(string fileName);
        string GetImageSecurityHash(string container, string imageName);
        string GetUploadImageSecurityKey(string container, string imageName, string imageSize);
        IImageEncoder GetImageEncoder(string fileFormat);
        Dictionary<string, long> GetImagesDictionaryPathAndSize(IContainerService container);
        Dictionary<string, CloudFileInfo> GetBaseImagesDictionary(IContainerService container);
        Dictionary<string, DateTime> GetCacheImagesDictionary(IContainerService container);
        MemoryStream MutateImage(MemoryStream imageFromStorage, IContainerService container, int width, int height, bool padding, string fileFormat, bool watermark);
        bool SaveImage(MemoryStream imageToSave, string imagePath, IContainerService container);
    
        ImageData UploadImage(Stream imageStream, IContainerService container, string imagePath);
        void RemoveOldCache(IContainerService container, int days);

    }
}
