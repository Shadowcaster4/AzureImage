using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Functions;
using ImageResizer.Services.Interfaces;
using ServiceStack.OrmLite;

namespace ImageResizer.Services
{
    public class DatabaseService : IDatabaseService
    {
        private OrmLiteConnectionFactory DbConnection { get; set; }
        private string DbConnString { get;  }
        private IImageService _service;

        public DatabaseService(string dbConnString,bool initialize)
        {
            DbConnString = dbConnString;
            if(initialize)
                Initialize();
            else
            {
                if (CheckIfDbFileExist(DbConnString))
                {
                    DbConnection = SetDbConnection(DbConnString);
                    _service =
                        Utilities.Utilities.GetImageService();//Environment.GetEnvironmentVariable("ApplicationEnvironment"));
                   
                }
                else
                {
                    CreateDatabase(DbConnString);
                    DbConnection = SetDbConnection(DbConnString);
                    _service =
                        Utilities.Utilities.GetImageService(); //GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

                }
            }
           
        }

        private void Initialize()
        {
            if (CheckIfDbFileExist(DbConnString))
            {
                DbConnection = SetDbConnection(DbConnString);
                _service =
                    Utilities.Utilities.GetImageService();//Environment.GetEnvironmentVariable("ApplicationEnvironment"));
            //    RestoreData(_service);
            //    CheckAndCorrectDbData(_service);
            }
            else
            {
                CreateDatabase(DbConnString);
                DbConnection = SetDbConnection(DbConnString);
                _service =
                    Utilities.Utilities.GetImageService(); //GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

                RestoreData(_service);
            }
        }

        public bool CheckIfDbFileExist(string databasePath)
        {
            return File.Exists(GetDbFilePathFromConnString(databasePath));
        }


        public void DeleteClientContainer(IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                db.Delete<ImageData>(x => x.ClientContainer == container.GetContainerName());
            }
        }

        public void DeleteImage(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                db.Delete<ImageData>(x =>
                    x.ImageName == imageName && x.ClientContainer == container.GetContainerName());
            }
        }

        public void DeleteLetterDirectory(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                db.Delete<ImageData>(x =>
                    x.ImageName.StartsWith(imageName[0]) && x.ClientContainer == container.GetContainerName());
            }
        }

        public ImageData GetImageData(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                //  return db.Select<ImageData>(x => x.ImageName == imageName).First(); //&& x.ClientContainer == container);
                return db.Single<ImageData>(x =>
                    x.ClientContainer == container.GetContainerName() && x.ImageName == imageName);
            }
        }

        public void RestoreData(IImageService imageService)
        {
            CreateTableIfNotExists();


            Parallel.ForEach(imageService.GetBlobContainers(),
                container => { RestoreDataForContainer(imageService, new ContainerClass(container)); });

            /*
            foreach (string container in service.GetBlobContainers())
            {
                RestoreDataForContainer(service,new ContainerClass(container)); 
            }
            
            */
        }

        public void SaveImagesData(List<ImageData> imageDataList)
        {
            using (var db = DbConnection.Open())
            {
                db.SaveAll(imageDataList);
            }
        }

        //todo:oddzielna metoda do sprawdzania rozmiaru obrazka + testy!!!! test szybkosc/wydajnosci w petli 1 plik 100 razy
        public void RestoreDataForContainer(IImageService imageService, IContainerService container)
        {
            var dbImagesList = GetContainerImagesDataFromDb(container);
            var storageImages = imageService.GetBaseImagesDictionary(container);

            var absentFromDb = storageImages.Keys.Except(dbImagesList.Select(x => x.ImageName)).ToList();

            if (absentFromDb.Any())
            {
                var iterator = 1;
                var tmpImageDataToUploadList = new List<ImageData>();
                foreach (var imageName in absentFromDb)
                {
                    tmpImageDataToUploadList.Add(GetImageProperties(imageService, imageName, container));
                    if (iterator % 300 == 0)
                    {
                        SaveImagesData(tmpImageDataToUploadList);
                        tmpImageDataToUploadList = new List<ImageData>();
                    }

                    iterator++;
                }

                SaveImagesData(tmpImageDataToUploadList);
            }
        }

        private OrmLiteConnectionFactory SetDbConnection(string dbConnString)
        {
            return new OrmLiteConnectionFactory(dbConnString, SqliteDialect.Provider);
        }

        public void CreateDatabase(string dbConnString)
        {
            using (var database = new SQLiteConnection(dbConnString))
            {
                database.Open();
            }
        }

        public void DropTable()
        {
            using (var db = DbConnection.Open())
            {
                db.DropTable<ImageData>();
            }
        }

        public string GetDbFilePathFromConnString(string dbConnString)
        {
            return dbConnString.Substring(dbConnString.IndexOf('=') + 1,
                dbConnString.IndexOf(';') - dbConnString.IndexOf('=') - 1);
        }

        public void DeleteImages(IEnumerable<ImageData> imagesToDelete)
        {
            using (var db = DbConnection.Open())
            {
                db.DeleteAll(imagesToDelete);
            }
        }

        public void CheckAndCorrectDbData(IImageService imageService)
        {
            Parallel.ForEach(imageService.GetBlobContainers(),
                container =>
                {
                    CompareAndCorrectDbDataWithStorageImages(imageService, new ContainerClass(container));
                });
        }


        public bool CreateTableIfNotExists()
        {
            using (var db = DbConnection.Open())
            {
                return db.CreateTableIfNotExists<ImageData>();
            }
        }

        public ImageData GetImageProperties(IImageService service, string imageName, IContainerService container)
        {
            var openImage = service.DownloadImageFromStorageToStream(
                service.GetImagePathUpload(imageName),
                container);
            openImage.Position = 0;

            var imageSize = GetFileResolution.GetDimensions(new BinaryReader(openImage));

            var imageData = new ImageData
            {
                ImageName = Path.GetFileName(imageName),
                ClientContainer = container.GetContainerName(),
                Width = imageSize.Width,
                Height = imageSize.Height,
                Size = openImage.Length.ToString()
            };
            openImage.Dispose();
            return imageData;
        }


        public void CompareAndCorrectDbDataWithStorageImages(IImageService imageService, IContainerService container)
        {
            var dbImagesList = GetContainerImagesDataFromDb(container);
            var storageImages = imageService.GetBaseImagesDictionary(container);
            var absentFromStorage = dbImagesList.Where(x => !storageImages.ContainsKey(x.ImageName));

            DeleteImages(absentFromStorage);
        }

        public List<ImageData> GetContainerImagesDataFromDb(IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                return DbConnection.Open().Select<ImageData>(x => x.ClientContainer == container.GetContainerName());
            }
        }
    }
}