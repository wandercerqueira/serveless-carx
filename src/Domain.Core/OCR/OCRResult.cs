namespace Domain.Core.OCR
{
    using Newtonsoft.Json;

    public class OCRResult
    {
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

        [JsonProperty(PropertyName = "textAngle")]
        public float TextAngle { get; set; }

        [JsonProperty(PropertyName = "orientation")]
        public string Orientation { get; set; }

        [JsonProperty(PropertyName = "regions")]
        public Region[] Regions { get; set; }
    }
}
