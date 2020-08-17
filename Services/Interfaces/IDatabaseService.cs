using System.Collections.Generic;
using ImageResizer.Services.Interfaces;
using System.Data;
using ImageResizer.Entities;

namespace ImageResizer.Database
{
    public interface IDatabaseService
    {
        
        void RestoreData(IImageService service);
        bool CheckIfDbFileExist(string databasePath); 
        void RestoreDataForContainer(IImageService service, IContainerService container);
        void DeleteImage(string imageName, IContainerService container);
        void DeleteClientContainer(IContainerService container);
        void DeleteLetterDirectory(string imageName, IContainerService container);
        ImageData GetImageData(string imageName, IContainerService container);
        void SaveImagesData(List<ImageData> imageDataList);
        void DropTable();
        bool CreateTableIfNotExists();
        void CheckAndCorrectDbData(IImageService imageService);
        void CompareAndCorrectDbDataForContainer(IImageService imageService, IContainerService container);
        long GetImagesInDbCount(IImageService service, IContainerService container);
        long GetImagesInDbCount(IImageService service);
    }
}