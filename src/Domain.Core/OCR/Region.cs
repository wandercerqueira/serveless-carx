namespace Domain.Core.OCR
{
    using Newtonsoft.Json;

    public class Region
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "lines")]
        public Line[] Lines { get; set; }
    }
}
