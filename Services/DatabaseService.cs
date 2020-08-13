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
        public IDbConnection dbConnection2 { get; }
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
               }
               else
               {               
              
                   CreateDatabase(dbConnString);
               DbConnection = SetDbConnection(DbConnString);
                   service =
                   Utilities.Utilities.GetImageService(Environment.GetEnvironmentVariable("ApplicationEnvironment"));

               CheckAndRestoreData(service);
        
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

        public void DeleteImages(string imageName, IContainerService container)
        {
            using (var db = DbConnection.Open())
            {
                db.Delete<ImageData>(x => x.ImageName == imageName && x.ClientContainer==container.GetContainerName());
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

        public void CheckAndRestoreData(IImageService service)
        {
            CreateTableIfNotExists();
            //todo:parallel tutaj 
            
           
            Parallel.ForEach(service.GetBlobContainers(), container =>
            {
                RestoreDataForContainer(service, new ContainerClass(container));
            });
            
            /*
            foreach (string container in service.GetBlobContainers())
            {
                RestoreDataForContainer(service,new ContainerClass(container)); 
            }
            
            */
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

       

        public void SaveImagesData(List<ImageData> imageDataList)
        {
            using (var db = DbConnection.Open())
            {
                db.SaveAll<ImageData>(imageDataList);
            }
        }

        public void RestoreDataForContainer(IImageService service, IContainerService container)
        {
            //todo: interfejs  kontener klienta - musi posiadac metode tabela w bazie danych //todo:orm
            var dbImagesList = DbConnection.Open().Select<ImageData>(x=>x.ClientContainer==container.GetContainerName());

            //todo: wywalic setservicecontainer
            //service.SetServiceContainer(container);
            var storageImages = service.GetBaseImagesDictionary(new ContainerClass(container.GetContainerName()));
            //todo:  storageImages // imagesinstorage
            //todo:  odwrotna sytaucja plik jest w bazie nie ma go na dysku 
            List<string> absentFromDb = storageImages.Keys.Except(dbImagesList.Select(x => x.ImageName)).ToList();

            if (absentFromDb.Any())
            {
                int iterator = 1;
                List<ImageData> tmpImageDataToUploadList = new List<ImageData>();
                foreach (var imageName in absentFromDb)
                { //todo:oddzielna metoda do sprawdzania rozmiaru obrazka + testy!!!! test szybkosc/wydajnosci w petli 1 plik 100 razy
               
                    tmpImageDataToUploadList.Add(GetImageProperties(service, imageName,container));
                    if (iterator % 200 == 0)
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
