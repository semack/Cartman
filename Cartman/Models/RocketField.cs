using Newtonsoft.Json;

namespace Cartman.Processor.Models
{
    public class RocketField
    {
        public RocketField()
        {
            Short = true;
        }
        [JsonProperty("short")]
        public bool Short { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
