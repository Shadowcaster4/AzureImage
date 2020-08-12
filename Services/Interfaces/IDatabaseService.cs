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
        void RestoreDataForContainer(IImageService service, string container);
        void DeleteImages(string imageName,string container);
        void DeleteClientContainer(string container);
        void DeleteLetterDirectory(string imageName, string container);
        ImageData GetImageData(string imageName, string container);
        void SaveImagesData(List<ImageData> imageDataList);
    }
}