using ImageResizer.Services.Interfaces;
using System.Data;

namespace ImageResizer.Database
{
    public interface IDatabaseService
    {
        void CheckAndRestoreData(IImageService service);
        bool CheckIfDbFileExist(string databasePath); 
        void RestoreDataForContainer(IImageService service, string container);
    }
}