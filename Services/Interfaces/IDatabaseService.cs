using System.Collections.Generic;
using ImageResizer.Services.Interfaces;
using System.Data;
using ImageResizer.Entities;

namespace ImageResizer.Database
{
    public interface IDatabaseService
    {
        IDbConnection dbConnection2 { get; }

        void CheckAndRestoreData(IImageService service);
        bool CheckIfDbFileExist(string databasePath); 
        void RestoreDataForContainer(IImageService service, IContainerService container);
        void DeleteImages(string imageName, IContainerService container);
        void DeleteClientContainer(IContainerService container);
        void DeleteLetterDirectory(string imageName, IContainerService container);
        ImageData GetImageData(string imageName, IContainerService container);
        void SaveImagesData(List<ImageData> imageDataList);
    }
}