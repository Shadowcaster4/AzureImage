﻿using Azure;
using Azure.Storage.Blobs.Models;
using ImageResizer.Entities;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ImageResizer.Database;

namespace ImageResizer.Services.Interfaces
{
    public interface IImageService
    {
        bool ChceckIfFileIsSupported(string fileName);
        bool CheckIfContainerExists(string containerName);
        bool CheckIfContainerNameIsValid(string containerName);
        bool CheckIfImageExists(string imagePath, string clientContainerName);
        bool CheckIfParametersAreInRange(int width, int height);
        bool CheckIfImageRequestedImageResolutionIsInRange(string imageName, int width, int height, ImageData imageData);
        bool CreateUsersContainer(IContainerService clientContainer);
        bool DeleteCachedImage(string imagePath, IContainerService container);
        bool DeleteClientContainer(IContainerService clientContainer);
        bool DeleteImageDirectory(string directoryName, IContainerService container);
        bool DeleteLetterDirectory(string fileName, IDbConnection dbConnection, IContainerService container);
        MemoryStream DownloadImageFromStorageToStream(string imagePath, IContainerService container);
        List<string> GetBlobContainers();
        string GetImagePathResize(QueryParameterValues parameters, string fileName);
        string GetImagePathUpload(string fileName);
        string GetImageExtension(string fileName);
        string GetImageSecurityHash(string container, string imageName);
        string GetUploadImageSecurityKey(string container, string imageName, string imageSize);
        IImageEncoder GetImageEncoder(string fileFormat);
        Dictionary<string, long> GetImagesDictionarySize(IContainerService container);
        Dictionary<string, CloudFileInfo> GetBaseImagesDictionary(IContainerService container);
        Dictionary<string, DateTime> GetCachedImagesDictionary(IContainerService container);
        MemoryStream MutateImage(MemoryStream imageFromStorage, IContainerService container, int width, int heigth, bool padding, string fileFormat, bool watermark);
        bool SaveImage(MemoryStream imageToSave, string imagePath, IContainerService container);
        bool SetImageObject(string imagePath, string clientContainerName);
        bool SetServiceContainer(string containerName);
        bool UploadImage(Stream image, IContainerService container, string imagePath, IDatabaseService dbService);

        string Test(string fileName);
    }
}
