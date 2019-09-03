namespace Application.Service
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Domain.Core;
    using Domain.Core.Licenses;

    public class SendToEventGrid
    {        
        private readonly string _uri = Environment.GetEnvironmentVariable("eventGridTopicEndpoint");
        private readonly string _key = Environment.GetEnvironmentVariable("eventGridTopicKey");

        private readonly HttpClient _client;
        private readonly ILogger _log;

        public SendToEventGrid(ILogger log, HttpClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task SendLicensePlateData(PlateDataLicense data)
        {
            if (data.PlateFound)
                // TODO 3: Modify send method to include the proper eventType name value for saving plate data.
                await Send("savePlateData", "TollBooth/CustomerService", data);
            else
                // TODO 4: Modify send method to include the proper eventType name value for queuing plate for manual review.
                await Send("queuePlateForManualCheckup", "TollBooth/CustomerService", data);            
        }

        private async Task Send(string eventType, string subject, PlateDataLicense data)
        {            
            _log.LogInformation($"Sending license plate data to the {eventType} Event Grid type");

            var events = new List<Event<PlateDataLicense>>
            {
                new Event<PlateDataLicense>()
                {
                    Data = data,
                    EventTime = DateTime.UtcNow,
                    EventType = eventType,
                    Id = Guid.NewGuid().ToString(),
                    Subject = subject
                }
            };

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("aeg-sas-key", _key);

            await _client.PostAsJsonAsync(_uri, events);

            _log.LogInformation($"Sent the following to the Event Grid topic: {events[0]}");
        }
    }
}
