using Azure;
using Azure.Storage.Blobs.Models;
using ImageResizer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        MemoryStream DownloadImageFromStorageToStream(string imagePath);
        Pageable<BlobContainerItem> GetBlobContainers();
        string GetImagePathResize(string parameters, string fileName);
        string GetImagePathUpload(string fileName);
        Dictionary<string, DateTimeOffset> GetImagesDictionaryDate();
        Dictionary<string, long> GetImagesDictionarySize();
        Pageable<BlobItem> GetImagesFromContainer();
        MemoryStream MutateImage(MemoryStream imageFromStorage, int width, int heigth, bool padding);
        bool SaveImage(MemoryStream imageToSave, string imagePath);
        bool SetImageObject(string imagePath);
        bool SetServiceContainer(string containerName);
        bool UploadImage(Stream image, string usersContainerName, string imagePath);

        public Dictionary<string, CloudFileInfo> GetBaseImagesDictionary();
        public Dictionary<string, DateTimeOffset> GetCachedImagesDictionary();

    }
}
