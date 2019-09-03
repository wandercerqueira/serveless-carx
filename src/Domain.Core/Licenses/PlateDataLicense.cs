namespace Domain.Core.Licenses
{
    using System;
    using Newtonsoft.Json;

    public class PlateDataLicense
    {
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "plateText")]
        public string PlateText { get; set; }

        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty(PropertyName = "plateFound")]
        public bool PlateFound => !string.IsNullOrWhiteSpace(PlateText);
    }
}
