using ImageResizer.Services.Interfaces;
using System.Data;

namespace ImageResizer.Database
{
    public interface IDatabaseService
    {
        IDbConnection dbConnection { get; }

        void CheckAndRestoreData(IImageService service);
        bool CheckIfDbFileExist(string databasePath);
        void CreateBackup(string databasePath);
        void RestoreDatabaseFromBackup(string databasePath);
        void RestoreDataForContainer(IImageService service, string container);
    }
}