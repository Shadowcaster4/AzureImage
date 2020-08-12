using ImageResizer.Services.Interfaces;
using System.Data;

namespace ImageResizer.Database
{
    public interface IDatabaseService
    {
        IDbConnection dbConnection2 { get; }

        void CheckAndRestoreData(IImageService service);
        bool CheckIfDbFileExist(string databasePath); 
        void RestoreDataForContainer(IImageService service, string container);
    }
}