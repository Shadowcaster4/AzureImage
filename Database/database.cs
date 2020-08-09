﻿using Dapper;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace ImageResizer.Database
{
    public class database
    {        
        public IDbConnection dbConnection { get; }

        public database()
        {
            try
            {
                string dbFilePath = Environment.GetEnvironmentVariable("DatabaseConnectionString");
                dbFilePath = dbFilePath.Substring(dbFilePath.IndexOf("."), dbFilePath.IndexOf(';') - dbFilePath.IndexOf("."));

                FileInfo fileInfo = new FileInfo(dbFilePath);
                if(fileInfo.Exists && fileInfo.Length>0)
                {
                    dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));
                }
                else
                {
                    if (CheckIfDbFileExist(dbFilePath + ".Bak"))
                        RestoreDatabaseFromBackup(dbFilePath);
                 
                    dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));

                    IImageService service;
                    if (Environment.GetEnvironmentVariable("ApplicationEnvironment") == "Local")
                        service = new ImageServiceLocally();
                    else
                        service = new ImageService();

                    CheckAndRestoreData(service);                   

                    CreateBackup(dbFilePath);                   

                }
            }
            catch (Exception e)
            {
                
                throw e;
            }
        }
        public bool CheckIfDbFileExist(string databasePath)
        {
            return File.Exists(databasePath);
        }

        public void CreateBackup(string databasePath)
        {
            if(CheckIfDbFileExist(databasePath+".Bak"))
                File.Copy(databasePath+".Bak", databasePath + "Old.Bak", true);
            File.Copy(databasePath, databasePath + ".Bak", true);
        }        

        public void RestoreDatabaseFromBackup(string databasePath)
        {
            File.Copy(databasePath+".Bak", databasePath , true);
        }

        public void CheckAndRestoreData(IImageService service)
        {
            foreach (string container in service.GetBlobContainers())
            {
                if (dbConnection.Query($"SELECT COUNT(tbl_name)  as 'amount' from sqlite_master where tbl_name = '{Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container}'").FirstOrDefault().amount == 0)
                    dbConnection.Execute($"CREATE TABLE \"{Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container}\" (\n\t\"Id\"\tINTEGER NOT NULL UNIQUE,\n\t\"ImageName\"\tTEXT NOT NULL UNIQUE,\n\t\"Width\"\tINTEGER NOT NULL,\n\t\"Height\"\tINTEGER NOT NULL,\n\t\"Size\"\tTEXT NOT NULL,\n\tPRIMARY KEY(\"Id\" AUTOINCREMENT)\n)");

                var dbImagesList = dbConnection.Query<List<string>>($"select imageName from {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container}  ", new DynamicParameters());

                service.SetServiceContainer(container);
                var appImages = service.GetBaseImagesDictionary();

                List<string> absentFromDb = appImages.Keys.Except(dbImagesList.Select(x => x.ToString())).ToList();

                if (absentFromDb.Count > 0)
                {
                    foreach (var imageName in absentFromDb)
                    {
                        using (var openImage = File.Open(Environment.GetEnvironmentVariable("LocalStorageConnectionString") +"\\"+container+"\\" + service.GetImagePathUpload(imageName), FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            openImage.Position = 0;
                            Image<Rgba32> imageObjCreatedForGettingImageData = (Image<Rgba32>)Image.Load(openImage);
                            ImageData imageData = new ImageData()
                            {
                                ImageName = Path.GetFileName(imageName),
                                Width = imageObjCreatedForGettingImageData.Width,
                                Height = imageObjCreatedForGettingImageData.Height,
                                Size = openImage.Length.ToString()
                            };
                            dbConnection.Execute($"insert into {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container} (imageName,width,height,size) values (@imageName,@width,@height,@size)", imageData);
                            imageObjCreatedForGettingImageData.Dispose();
                        }
                    }
                }
            }
        }


    }
}