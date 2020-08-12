using Dapper;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace ImageResizer.Services
{
    public class DatabaseService:IDatabaseService
    {
        public IDbConnection dbConnection2 { get; }
        private  OrmLiteConnectionFactory DbConnection { get; }
        private string DbConnString { get;}



        public DatabaseService(string dbConnString)
        {
               DbConnString = dbConnString;
               if(CheckIfDbFileExist(DbConnString))
               {
                    DbConnection = SetDbConnection(DbConnString);
               }
               else
               {               
              
                   CreateDatabase(dbConnString);
               DbConnection = SetDbConnection(DbConnString);
               IImageService service =
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

        public void CheckAndRestoreData(IImageService service)
        {
            //todo:parallel tutaj 
            CreateTableIfNotExists();
            foreach (string container in service.GetBlobContainers())
            {
                RestoreDataForContainer(service, container);
            }
        }

        public bool CreateTableIfNotExists()
        {
            return DbConnection.Open().CreateTableIfNotExists<ImageData>();
        }

        public ImageData GetImageProperties(IImageService service,string imageName,string container)
        {
            MemoryStream openImage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(imageName));
            openImage.Position = 0;
            Image<Rgba32> imageObjCreatedForGettingImageData = (Image<Rgba32>)Image.Load(openImage);
            ImageData imageData = new ImageData()
            {
                ImageName = Path.GetFileName(imageName),
                ClientContainer = container,
                Width = imageObjCreatedForGettingImageData.Width,
                Height = imageObjCreatedForGettingImageData.Height,
                Size = openImage.Length.ToString()
            };
            imageObjCreatedForGettingImageData.Dispose();
            return imageData;
        }

    

        public void SaveImagesData(List<ImageData> imageDataList)
        {
            using (var db = DbConnection.Open())
            {
                db.SaveAll<ImageData>(imageDataList);
            }
        }

        public void RestoreDataForContainer(IImageService service, string container)
        {
            //todo: interfejs  kontener klienta - musi posiadac metode tabela w bazie danych //todo:orm
            var dbImagesList = DbConnection.Open().Select<ImageData>(x=>x.ClientContainer==container);

            //todo: wywalic setservicecontainer
            service.SetServiceContainer(container);
            var storageImages = service.GetBaseImagesDictionary();
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
                    if (iterator % 100 == 0)
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
