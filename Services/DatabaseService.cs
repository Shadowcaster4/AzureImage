using Dapper;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageResizer.Functions;
using SixLabors.ImageSharp.Formats.Jpeg;
using Size = SixLabors.ImageSharp.Size;

namespace ImageResizer.Services
{
    public class DatabaseService:IDatabaseService
    {
        
        private  OrmLiteConnectionFactory DbConnection { get; }
        private string DbConnString { get;}
        private IImageService service;

        public DatabaseService(string dbConnString)
        {
               DbConnString = dbConnString;
               if(CheckIfDbFileExist(DbConnString))
               {
                    DbConnection = SetDbConnection(DbConnString);
                    service =
                        Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));
                    RestoreData(service);
                    CheckAndCorrectDbData(service);
               }
               else
               {               
              
                   CreateDatabase(dbConnString);
                   DbConnection = SetDbConnection(DbConnString);
                   service =
                   Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

               RestoreData(service);
        
               }
        }

        private OrmLiteConnectionFactory SetDbConnection(string dbConnString)
        {
           return new OrmLiteConnectionFactory(dbConnString, SqliteDialect.Provider);
        }

        public void CreateDatabase(string dbConnString)
        {
            using(SQLiteConnection database = new System.Data.SQLite.SQLiteConnection(dbConnString))
            {
                database.Open();
            }
        }
        public string GetDbFilePathFromConnString(string dbConnString)
        {
            return dbConnString.Substring(dbConnString.IndexOf('=')+1, dbConnString.IndexOf(';')- dbConnString.IndexOf('=') - 1);
            
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
              db.Delete<ImageData>(x => x.ImageName == imageName && x.ClientContainer==container.GetContainerName());
            }
        }

        public void DeleteImages(IEnumerable<ImageData> imagesToDelete)
        {
            using (var db = DbConnection.Open())
            {
                db.DeleteAll<ImageData>(imagesToDelete);
            }
        }

        public void DeleteLetterDirectory(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                db.Delete<ImageData>(x => x.ImageName.StartsWith(imageName[0]) && x.ClientContainer == container.GetContainerName());
            }
        }

        public ImageData GetImageData(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
              //  return db.Select<ImageData>(x => x.ImageName == imageName).First(); //&& x.ClientContainer == container);
              return db.Single<ImageData>(x=>x.ClientContainer==container.GetContainerName() && x.ImageName==imageName);
              
            }
        }

        public void RestoreData(IImageService imageService)
        {
            CreateTableIfNotExists();
           
           
            Parallel.ForEach(imageService.GetBlobContainers(), container =>
            {
                RestoreDataForContainer(imageService, new ContainerClass(container));
            });
            
            /*
            foreach (string container in service.GetBlobContainers())
            {
                RestoreDataForContainer(service,new ContainerClass(container)); 
            }
            
            */
        }

        public void CheckAndCorrectDbData(IImageService imageService)
        {
            Parallel.ForEach(imageService.GetBlobContainers(), container =>
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

        public ImageData GetImageProperties(IImageService service,string imageName, IContainerService container)
        {
            MemoryStream openImage = service.DownloadImageFromStorageToStream(
                service.GetImagePathUpload(imageName),
                container);
            openImage.Position = 0;

            Size imageSize = GetFileResolution.GetDimensions(new BinaryReader(openImage));

            ImageData imageData = new ImageData()
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
            IEnumerable<ImageData> absentFromStorage = dbImagesList.Where(x => !storageImages.ContainsKey(x.ImageName));

            DeleteImages(absentFromStorage);
            
        }

        public void SaveImagesData(List<ImageData> imageDataList)
        {
            using (var db = DbConnection.Open())
            {
                db.SaveAll<ImageData>(imageDataList);
            }
        }

        public List<ImageData> GetContainerImagesDataFromDb(IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                return DbConnection.Open().Select<ImageData>(x => x.ClientContainer == container.GetContainerName());
            }
        }

        //todo:oddzielna metoda do sprawdzania rozmiaru obrazka + testy!!!! test szybkosc/wydajnosci w petli 1 plik 100 razy
        public void RestoreDataForContainer(IImageService imageService, IContainerService container)
        {
            var dbImagesList = GetContainerImagesDataFromDb(container);
            var storageImages = imageService.GetBaseImagesDictionary(container);
            
            List<string> absentFromDb = storageImages.Keys.Except(dbImagesList.Select(x => x.ImageName)).ToList();

            if (absentFromDb.Any())
            {
                int iterator = 1;
                List<ImageData> tmpImageDataToUploadList = new List<ImageData>();
                foreach (var imageName in absentFromDb)
                {
                    tmpImageDataToUploadList.Add(GetImageProperties(imageService, imageName,container));
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
    }
}
