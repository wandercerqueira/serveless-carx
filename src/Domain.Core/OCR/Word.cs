namespace Domain.Core.OCR
{
    using Newtonsoft.Json;

    public class Word
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}
