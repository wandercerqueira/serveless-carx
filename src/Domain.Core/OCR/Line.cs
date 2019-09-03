namespace Domain.Core.OCR
{
    using Newtonsoft.Json;

    public class Line
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "words")]
        public Word[] Words { get; set; }
    }
}
