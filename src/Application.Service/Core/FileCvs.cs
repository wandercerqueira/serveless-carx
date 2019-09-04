namespace Application.Service.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CsvHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Domain.Core.Licenses;

    public sealed class FileCsv
    {
        private readonly string _containerName = Environment.GetEnvironmentVariable("exportCsvContainerName");
        private readonly string _blobStorageConnection = Environment.GetEnvironmentVariable("blobStorageConnection");

        private readonly ILogger _log;
        private readonly CloudBlobClient _blobClient;

        public FileCsv(ILogger log)
        {
            var storageAccount = CloudStorageAccount.Parse(_blobStorageConnection);

            _log = log;
            _blobClient = storageAccount.CreateCloudBlobClient();            
        }

        public async Task<bool> GenerateAndSaveCsv(IEnumerable<PlateDataDocumentLicense> plates)
        {
            _log.LogInformation("Generating CSV file");

            var successful = false;            
            string blobName = $"{DateTime.UtcNow:s}.csv";

            using (var stream = new MemoryStream())
            {
                using (var textWriter = new StreamWriter(stream))
                using (var csv = new CsvWriter(textWriter))
                {
                    csv.Configuration.Delimiter = ",";
                    csv.WriteRecords(plates.Select(ToPlateDataLicense));
                    await textWriter.FlushAsync();

                    _log.LogInformation($"Beginning file upload: {blobName}");

                    try
                    {
                        var container = _blobClient.GetContainerReference(_containerName);
                        var blob = container.GetBlockBlobReference(blobName);

                        await container.CreateIfNotExistsAsync();
                                                
                        stream.Position = 0;                        
                        // TODO 7: Asyncronously upload the blob from the memory stream.

                        successful = true;

                        _log.LogInformation("Upload Successfully");
                    }
                    catch (Exception e)
                    {
                        _log.LogCritical($"Could not upload CSV file: {e.Message}", e);
                        successful = false;
                    }
                }
            }

            return successful;
        }
      
        private static PlateDataLicense ToPlateDataLicense(PlateDataDocumentLicense source)
        {
            return new PlateDataLicense
            {
                FileName = source.fileName,
                PlateText = source.plateText,
                TimeStamp = source.timeStamp
            };
        }
    }
}
