using Dapper;
using ImageResizer.Entities;
using ImageResizer.Services;
using ImageResizer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

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
                    fileInfo = new FileInfo(dbFilePath+".Bak");
                    if (fileInfo.Exists)
                        fileInfo.CopyTo(dbFilePath);
                    dbConnection = new SQLiteConnection(Environment.GetEnvironmentVariable("DatabaseConnectionString"));

                    IImageService service;
                    if (Environment.GetEnvironmentVariable("ApplicationEnvironment") == "Local")
                        service = new ImageServiceLocally();
                    else
                        service = new ImageService();

                    foreach (string container in service.GetBlobContainers())
                    {
                        var dbImagesList = dbConnection.Query<string[]>($"select 'imageName' from {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + container} ' ", new DynamicParameters());

                        service.SetServiceContainer(container);
                        var appImages = service.GetBaseImagesDictionary();

                        var absentFromDb = appImages.Keys.Except(dbImagesList);
                    }

                    ImageData imageData = dbConnection.Query<ImageData>($"select * from {Environment.GetEnvironmentVariable("SQLiteBaseTableName") + userContainerName} where imageName='{imageName}' ", new DynamicParameters()).FirstOrDefault();

                }




            }
            catch (Exception e)
            {
                
                throw e;
            }
        }

    }
}
