namespace Application.Service.Core
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Extensions.Logging;
    using Polly;
    using Polly.CircuitBreaker;
    using Polly.Wrap;
    using Domain.Core.OCR;

    public sealed class FindLicensePlateText
    {
        private readonly string _requestParameters = "language=unk&detectOrientation=true";

        // Get the API URL and the API key from settings.
        // TODO 2: Populate the below two variables with the correct AppSettings properties.
        private readonly string _uriBase = Environment.GetEnvironmentVariable("computerVisionApiUrl");
        private readonly string _apiKey = Environment.GetEnvironmentVariable("computerVisionApiKey");        

        private readonly ILogger _log;
        private readonly HttpClient _client;

        public FindLicensePlateText(ILogger log, HttpClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task<string> GetLicensePlate(byte[] imageBytes)
        {
            return await MakeOCRRequest(imageBytes);
        }

        private async Task<string> MakeOCRRequest(byte[] imageBytes)
        {
            _log.LogInformation("Making OCR request");

            var resiliencyStrategy = DefineAndRetrieveResiliencyStrategy();
                        
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var licensePlate = string.Empty;

            try
            {
                HttpResponseMessage response = await resiliencyStrategy.ExecuteAsync(() => _client.PostAsync($"{_uriBase}?{_requestParameters}", GetImageHttpContent(imageBytes)));

                // Get the JSON response.
                var result = await response.Content.ReadAsAsync<OCRResult>();
                licensePlate = GetLicensePlateTextFromResult(result);
            }
            catch (BrokenCircuitException bce)
            {
                _log.LogCritical($"Could not contact the Computer Vision API service due to the following error: {bce.Message}");
            }
            catch (Exception e)
            {
                _log.LogCritical($"Critical error: {e.Message}", e);
            }

            _log.LogInformation($"Finished OCR request. Result: {licensePlate}");

            return licensePlate;
        }

        
        private static ByteArrayContent GetImageHttpContent(byte[] imageBytes)
        {
            var content = new ByteArrayContent(imageBytes);            
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return content;
        }

        private static string GetLicensePlateTextFromResult(OCRResult result)
        {
            var text = string.Empty;
            if (result.Regions == null || result.Regions.Length == 0) return string.Empty;

            const string states = "ALABAMA,ALASKA,ARIZONA,ARKANSAS,CALIFORNIA,COLORADO,CONNECTICUT,DELAWARE,FLORIDA,GEORGIA,HAWAII,IDAHO,ILLINOIS,INDIANA,IOWA,KANSAS,KENTUCKY,LOUISIANA,MAINE,MARYLAND,MASSACHUSETTS,MICHIGAN,MINNESOTA,MISSISSIPPI,MISSOURI,MONTANA,NEBRASKA,NEVADA,NEW HAMPSHIRE,NEW JERSEY,NEW MEXICO,NEW YORK,NORTH CAROLINA,NORTH DAKOTA,OHIO,OKLAHOMA,OREGON,PENNSYLVANIA,RHODE ISLAND,SOUTH CAROLINA,SOUTH DAKOTA,TENNESSEE,TEXAS,UTAH,VERMONT,VIRGINIA,WASHINGTON,WEST VIRGINIA,WISCONSIN,WYOMING";
            string[] chars = { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
            var stateList = states.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in result.Regions[0].Lines.Take(2))
            {
                if (stateList.Contains(line.Words[0].Text.ToUpper())) continue;

                foreach (var word in line.Words)
                    if (!string.IsNullOrWhiteSpace(word.Text))
                        text += (RemoveSpecialCharacters(word.Text)) + " ";
            }

            return text.ToUpper().Trim();
        }

        private static string RemoveSpecialCharacters(string str)
        {
            int idx = 0;
            var buffer = new char[str.Length];

            foreach (var c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z') || (c == '-'))
                {
                    buffer[idx] = c;
                    idx++;
                }
            }

            return new string(buffer, 0, idx);
        }
      
        private AsyncPolicyWrap<HttpResponseMessage> DefineAndRetrieveResiliencyStrategy()
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
               HttpStatusCode.InternalServerError, // 500
               HttpStatusCode.BadGateway, // 502
               HttpStatusCode.GatewayTimeout // 504
            };

            HttpStatusCode[] httpStatusCodesToImmediatelyFail = {
               HttpStatusCode.BadRequest, // 400
               HttpStatusCode.Unauthorized, // 401
               HttpStatusCode.Forbidden // 403
            };

            // Define our waitAndRetry policy: retry n times with an exponential backoff in case the Computer Vision API throttles us for too many requests.
            var waitAndRetryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(e => e.StatusCode == HttpStatusCode.ServiceUnavailable || e.StatusCode == (HttpStatusCode)429 || e.StatusCode == (HttpStatusCode)403)
                .WaitAndRetryAsync(
                    10,                                                           // Retry 10 times with a delay between retries before ultimately giving up
                    attempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, attempt)), // Back off!  2, 4, 8, 16 etc times 1/4-second
                                                                                  // attempt => TimeSpan.FromSeconds(6), Wait 6 seconds between retries
                    (exception, calculatedWaitDuration) =>
                    {
                        _log.LogWarning($"Computer Vision API server is throttling our requests. Automatically delaying for {calculatedWaitDuration.TotalMilliseconds}ms");
                    }
                );

            // Define our first CircuitBreaker policy: Break if the action fails 4 times in a row. This is designed to handle Exceptions from the Computer Vision API, as well as
            // a number of recoverable status messages, such as 500, 502, and 504.
            var circuitBreakerPolicyForRecoverable = Policy
                .Handle<HttpResponseException>()
                .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (outcome, breakDelay) =>
                    {
                        _log.LogWarning($"Polly Circuit Breaker logging: Breaking the circuit for {breakDelay.TotalMilliseconds}ms due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                    },
                    onReset: () => _log.LogInformation("Polly Circuit Breaker logging: Call ok... closed the circuit again"),
                    onHalfOpen: () => _log.LogInformation("Polly Circuit Breaker logging: Half-open: Next call is a trial")
                );
            
            return Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicyForRecoverable);
        }
    }
}
