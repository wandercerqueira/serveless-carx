namespace Application.Service.Functions
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Application.Service.Core;
    
    public static class ExportLicensePlates
    {
        [FunctionName("ExportLicensePlates")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("Finding license plate data to export");

            int exportedCount = 0;

            var databaseMethods = new DataBase(log);
            var licensePlates = databaseMethods.GetLicensePlatesToExport();

            if (licensePlates.Any())
            {
                log.LogInformation($"Retrieved {licensePlates.Count} license plates");

                var fileMethods = new FileCsv(log);
                var uploaded = await fileMethods.GenerateAndSaveCsv(licensePlates);

                if (uploaded)
                {
                    await databaseMethods.MarkLicensePlatesAsExported(licensePlates);
                    exportedCount = licensePlates.Count;
                    log.LogInformation("Finished updating the license plates");
                }
                else
                {
                    log.LogInformation(
                        "Export file could not be uploaded. Skipping database update that marks the documents as exported.");
                }

                log.LogInformation($"Exported {exportedCount} license plates");
            }
            else
                log.LogWarning("No license plates to export");

            return exportedCount == 0
                ? req.CreateResponse(HttpStatusCode.NoContent)
                : req.CreateResponse(HttpStatusCode.OK, $"Exported {exportedCount} license plates");
        }
    }
}
