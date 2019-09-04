namespace UploadLicensePlates
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.DataMovement;
    using Microsoft.Extensions.Configuration;
   
    internal class Program
    {
        private static List<MemoryStream> _sourceImages;
        private static readonly Random Random = new Random();
        private static string blobStorageConnection;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("conf/appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            
            blobStorageConnection = configuration.GetSection("blobStorageConnection").Value.ToString();
            
            int choice = 1;

            Console.WriteLine("Enter one of the following numbers to indicate what type of image upload you want to perform:");
            Console.WriteLine("\t1 - Upload a handful of test photos");
            Console.WriteLine("\t2 - Upload 1000 photos to test processing at scale");

            int.TryParse(Console.ReadLine(), out choice);

            bool upload1000 = choice == 2;

            UploadImages(upload1000);

            Console.ReadLine();
        }

        #region Private Methods

        private static void UploadImages(bool upload1000)
        {
            Console.WriteLine("Uploading images");

            TransferManager.Configurations.ParallelOperations = 64;
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 8;
            ServicePointManager.Expect100Continue = false;
            
            var account = CloudStorageAccount.Parse(blobStorageConnection);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("images");

            blobContainer.CreateIfNotExistsAsync();
            
            if (upload1000)
            {
                LoadImagesFromDisk(true);
                for (var i = 0; i < 200; i++)
                    UploadFiles(blobContainer);
            }
            else
            {
                LoadImagesFromDisk(false);
                UploadFiles(blobContainer);                
            }

            Console.WriteLine("Finished uploading images");
        }

        private static void UploadFiles(CloudBlobContainer blobContainer) {
            int uploaded = 0;

            foreach (var image in _sourceImages)
            {
                var filename = GenerateRandomFileNameLicensePlates();
                var destBlob = blobContainer.GetBlockBlobReference(filename);

                var task = TransferManager.UploadAsync(image, destBlob);
                task.Wait();

                uploaded++;

                Console.WriteLine($"Uploaded image {uploaded}: {filename}");
            }
        }

        private static string GenerateRandomFileNameLicensePlates()
        {
            const int randomStringLength = 8;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var rando = new string(Enumerable.Repeat(chars, randomStringLength)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
            return $"{rando}.jpg";
        }
        
        private static void LoadImagesFromDisk(bool upload1000)
        {
            if (upload1000)
            {
                _sourceImages =
                    Directory.GetFiles(@"..\..\..\..\license plates\copyfrom\")
                        .Select(f => new MemoryStream(File.ReadAllBytes(f)))
                        .ToList();
            }
            else
            {
                _sourceImages =
                    Directory.GetFiles(@"..\..\..\..\license plates\")
                        .Select(f => new MemoryStream(File.ReadAllBytes(f)))
                        .ToList();
            }
        }

        #endregion
    }
}
