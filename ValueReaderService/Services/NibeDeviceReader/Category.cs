using System.Text.Json.Serialization;

namespace ValueReaderService.Services.NibeDeviceReader
{
    public class Category
    {
        [JsonPropertyName("categroyId")]
        public int CategroyId { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("parameters")]
        public Parameter[]? Parameters { get; set; }
    }
}
