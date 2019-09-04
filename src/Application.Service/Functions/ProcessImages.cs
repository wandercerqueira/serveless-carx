namespace Application.Service.Functions
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Azure.EventGrid.Models;
    using Microsoft.Azure.WebJobs.Extensions.EventGrid;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Newtonsoft.Json.Linq;
    using Application.Service.Core;
    using Domain.Core.Licenses;

    public static class ProcessImage
    {
        [FunctionName("ProcessImage")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent,
            [Blob(blobPath: "{data.url}", access: FileAccess.Read, Connection = "blobStorageConnection")] Stream incomingPlate, ILogger log)
        {
            var plateText = string.Empty;

            //_client = _client ?? new HttpClient();
            var _client = new HttpClient();

            try
            {
                if (incomingPlate != null)
                {
                    var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                    var name = GetBlobNameFromUrl(createdEvent.Url);

                    log.LogInformation($"Processing {name}");

                    byte[] plateImage;

                    using (var br = new BinaryReader(incomingPlate))
                    {
                        plateImage = br.ReadBytes((int)incomingPlate.Length);
                    }

                    // TODO 1: Set the licensePlateText value by awaiting a new FindLicensePlateText.GetLicensePlate method.

                    // Send the details to Event Grid.
                    await new SendToEventGrid(log, _client).SendLicensePlateData(new PlateDataLicense()
                    {
                        FileName = name,
                        PlateText = plateText,
                        TimeStamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.Message);
                throw;
            }

            log.LogInformation($"Finished processing. Detected the following license plate: {plateText}");
        }       

        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var cloudBlob = new CloudBlob(uri);

            return cloudBlob.Name;
        }
    }
}
