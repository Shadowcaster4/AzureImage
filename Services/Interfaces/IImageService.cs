using Azure;
using Azure.Storage.Blobs.Models;
using ImageResizer.Entities;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageResizer.Services.Interfaces
{
    public interface IImageService
    {
        bool ChceckIfFileIsSupported(string fileName);
        bool CheckIfContainerExists(string containerName);
        bool CheckIfContainerNameIsValid(string containerName);
        bool CheckIfImageExists(string imagePath);
        bool CheckIfParametersAreInRange(int width, int height);
        bool CreateUsersContainer(string clientContainerName);
        bool DeleteCachedImage(string imagePath);
        bool DeleteClientContainer(string clientContainerName);
        bool DeleteImageDirectory(string directoryName);
        bool DeleteLetterDirectory(string fileName);
        MemoryStream DownloadImageFromStorageToStream(string imagePath);
        Pageable<BlobContainerItem> GetBlobContainers();
        string GetImagePathResize(string parameters, string fileName);
        string GetImagePathUpload(string fileName);
        string GetImageExtension(string fileName);
        IImageEncoder GetImageEncoder(string fileFormat);
        Dictionary<string, long> GetImagesDictionarySize();
        Dictionary<string, CloudFileInfo> GetBaseImagesDictionary();
        Dictionary<string, DateTimeOffset> GetCachedImagesDictionary();
        Pageable<BlobItem> GetImagesFromContainer();
        MemoryStream MutateImage(MemoryStream imageFromStorage, int width, int heigth, bool padding, string fileFormat);
        bool SaveImage(MemoryStream imageToSave, string imagePath);
        bool SetImageObject(string imagePath);
        bool SetServiceContainer(string containerName);
        bool UploadImage(Stream image, string usersContainerName, string imagePath);
        

    }
}
