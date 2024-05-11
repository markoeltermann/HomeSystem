using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ValueReaderService.Services.NibeDeviceReader
{
    public class Parameter
    {
        [JsonPropertyName("parameterId")]
        public int ParameterId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("displayValue")]
        public string? DisplayValue { get; set; }

        [JsonPropertyName("rawValue")]
        public int RawValue { get; set; }
    }
}
