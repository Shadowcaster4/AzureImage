using Dapper;
using ImageResizer.Database;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp;
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
        public OrmLiteConnectionFactory dbConnection { get; }

        public DatabaseService()
        {
               string dbConnString = Environment.GetEnvironmentVariable("DatabaseConnectionString");


               if(CheckIfDbFileExist(dbConnString))
               {
                    dbConnection= new OrmLiteConnectionFactory(dbConnString,SqliteDialect.Provider)
               )
               else
               {

               }
           
               dbConnection = 

               
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
        }

        public string GetDbFilePathFromConnString(string dbConnString)
        {
            return dbConnString.Substring(0, dbConnString.IndexOf(';'));
        }

        public bool CheckIfDbFileExist(string databasePath)
        {
            return File.Exists(GetDbFilePathFromConnString(databasePath));
        }

        public void CheckAndRestoreData(IImageService service)
        {
            //todo:parallel tutaj 
            foreach (string container in service.GetBlobContainers().Where(x=>!x.Contains("azure-webjobs")))
            {
                RestoreDataForContainer(service, container);
            }
        }


        public void RestoreDataForContainer(IImageService service, string container)
        {
            //todo: interfejs  kontener klienta - musi posiadac metode tabela w bazie danych
            dbConnection.Execute($"CREATE TABLE if not exists '{(Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container)}' (Id INTEGER NOT NULL UNIQUE,ImageName TEXT NOT NULL UNIQUE,Width INTEGER NOT NULL,Height INTEGER NOT NULL,Size TEXT NOT NULL, PRIMARY KEY(Id AUTOINCREMENT))");
            //todo:orm
            var dbImagesList = dbConnection.Query<string>($"select imageName from '{Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container}'  ", new DynamicParameters());

            //todo: wywalic setservicecontainer
            service.SetServiceContainer(container);
            var storageImages = service.GetBaseImagesDictionary();
            //todo:  storageImages // imagesinstorage
            //todo:  odwrotna sytaucja plik jest w bazie nie ma go na dysku 
            List<string> absentFromDb = storageImages.Keys.Except(dbImagesList.Select(x => x.ToString())).ToList();

            if (absentFromDb.Any())
            {
                foreach (var imageName in absentFromDb)
                { //todo:oddzielna metoda do sprawdzania rozmiaru obrazka + testy!!!! test szybkosc/wydajnosci w petli 1 plik 100 razy
                    //
                        MemoryStream openImage = service.DownloadImageFromStorageToStream(service.GetImagePathUpload(imageName));
                        openImage.Position = 0;
                        Image<Rgba32> imageObjCreatedForGettingImageData = (Image<Rgba32>)Image.Load(openImage);
                        ImageData imageData = new ImageData()
                        {
                            ImageName = Path.GetFileName(imageName),
                            Width = imageObjCreatedForGettingImageData.Width,
                            Height = imageObjCreatedForGettingImageData.Height,
                            Size = openImage.Length.ToString()
                        };
                    //todo:ladowanie do pamieci lista obiektow po skonczonej petli duze zapytanie bulk do bazy danych ewentualny iterator i paczki np po 100 rekordow 
                    //
                        dbConnection.Execute($"insert into {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container} (imageName,width,height,size) values (@imageName,@width,@height,@size)", imageData);
                        imageObjCreatedForGettingImageData.Dispose();
                }
            }
        }
    }
}
