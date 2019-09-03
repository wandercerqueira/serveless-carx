namespace Application.Service.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Logging;
    using Domain.Core.Licenses;

    public sealed class DataBase
    {
        private readonly string _endpointUrl = Environment.GetEnvironmentVariable("cosmosDBEndPointUrl");
        private readonly string _authorizationKey = Environment.GetEnvironmentVariable("cosmosDBAuthorizationKey");
        private readonly string _databaseId = Environment.GetEnvironmentVariable("cosmosDBDatabaseId");
        private readonly string _collectionId = Environment.GetEnvironmentVariable("cosmosDBCollectionId");

        private readonly ILogger _log;
        private DocumentClient _client;

        public DataBase(ILogger log)
        {
            _log = log;            
        }

        public List<PlateDataDocumentLicense> GetLicensePlatesToExport()
        {
            _log.LogInformation("Retrieving license plates to export");

            int exportedCount = 0;
            var collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

            List<PlateDataDocumentLicense> licensePlates;

            using (_client = new DocumentClient(new Uri(_endpointUrl), _authorizationKey))
            {
                // MaxItemCount value tells the document query to retrieve 100 documents at a time until all are returned.
                // TODO 5: Retrieve a List of LicensePlateDataDocument objects from the collectionLink where the exported value is false.
                licensePlates = _client.CreateDocumentQuery<PlateDataDocumentLicense>(collectionLink,
                new FeedOptions() { EnableCrossPartitionQuery = true, MaxItemCount = 100 })
                .Where(l => l.exported == false)
                .ToList();
            }

            // TODO 6: Remove the line below.
            // licensePlates = new List<PlateDataDocumentLicense>();

            exportedCount = licensePlates.Count();
            _log.LogInformation($"{exportedCount} license plates found that are ready for export");

            return licensePlates;
        }

        public async Task MarkLicensePlatesAsExported(IEnumerable<PlateDataDocumentLicense> licensePlates)
        {
            _log.LogInformation("Updating license plate documents exported values to true");
            
            foreach (var licensePlate in licensePlates)
            {
                licensePlate.Exported = true;
                var response = await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, licensePlate.Id), licensePlate);

                var updated = response.Resource;
                _log.LogInformation($"Exported value of updated document: {updated.GetPropertyValue<bool>("exported")}");
            }            
        }
    }
}
