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
        public OrmLiteConnectionFactory dbConnection { get; }

        public DatabaseService()
        {
               string dbConnString = @Environment.GetEnvironmentVariable("DatabaseConnectionString");


               if(CheckIfDbFileExist(dbConnString))
               {
                
                   dbConnection = new OrmLiteConnectionFactory(dbConnString, SqliteDialect.Provider);
               /*}
               else
               {
              */
              // CreateDatabase(dbConnString);
              

               IImageService service;
               if (Environment.GetEnvironmentVariable("ApplicationEnvironment") == "Local")
                    service = new ImageServiceLocally();
               else
                    service = new ImageService();                
                
               CheckAndRestoreData(service);
               dbConnection = new OrmLiteConnectionFactory(dbConnString, SqliteDialect.Provider);
               
               }

            #region smiec
            /*
            try
            {
                string dbFilePath = Environment.GetEnvironmentVariable("DatabaseConnectionString");
                dbFilePath = dbFilePath.Substring(dbFilePath.IndexOf("."), dbFilePath.IndexOf(';') - dbFilePath.IndexOf("."));

                FileInfo fileInfo = new FileInfo(dbFilePath);
                if (fileInfo.Exists && fileInfo.Length > 0)
                {
                    dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));
                }
                else
                {
                    
                    dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));

                    IImageService service;
                    if (Environment.GetEnvironmentVariable("ApplicationEnvironment") == "Local")
                        service = new ImageServiceLocally();
                    else
                        service = new ImageService();

                    CheckAndRestoreData(service);

                

                }
            }
            catch (Exception e)
            {
                throw e;
            }
            */
            #endregion
        }

        public void CreateDatabase(string dbConnString)
        {

            SQLiteConnection database =  new System.Data.SQLite.SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            var xyz = database.FileName;

        }
        public string GetDbFilePathFromConnString(string dbConnString)
        {
            string con = dbConnString.Substring(dbConnString.IndexOf('=')+1, dbConnString.IndexOf(';')- dbConnString.IndexOf('=') - 1);
            return con;
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
            return dbConnection.Open().CreateTableIfNotExists<ImageData>();
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

        public void InsertSingleImageData(ImageData imageDataToInsert)
        {
            dbConnection.Open().Insert<ImageData>(imageDataToInsert);                
        }

        public void InsertMultipleImagesData(List<ImageData> imageDataList)
        {
            dbConnection.Open().InsertAll<ImageData>(imageDataList);
        }

        public void RestoreDataForContainer(IImageService service, string container)
        {
            //todo: interfejs  kontener klienta - musi posiadac metode tabela w bazie danych
           
            //todo:orm
            var dbImagesList = dbConnection.Open().Select<ImageData>(x=>x.ClientContainer==container);

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
                  //
                    tmpImageDataToUploadList.Add(GetImageProperties(service, imageName,container));
                    //todo:ladowanie do pamieci lista obiektow po skonczonej petli duze zapytanie bulk do bazy danych ewentualny iterator i paczki np po 100 rekordow 
                    //
                    if (iterator % 100 == 0)
                    {
                        InsertMultipleImagesData(tmpImageDataToUploadList);
                        tmpImageDataToUploadList = new List<ImageData>();
                    }
                   
                    iterator++;
                }
                InsertMultipleImagesData(tmpImageDataToUploadList);
                tmpImageDataToUploadList = new List<ImageData>();
            }
        }
    }
}
