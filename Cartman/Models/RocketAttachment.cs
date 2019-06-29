using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cartman.Models
{
    public class RocketAttachment
    {
        public RocketAttachment()
        {
            Fields = new List<RocketField>();
        }

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("title_link")] public string TitleLink { get; set; }

        [JsonProperty("image_url")] public string ImageUrl { get; set; }

        [JsonProperty("message_link")] public string MessageLink { get; set; }

        [JsonProperty("text")] public string Text { get; set; }

        [JsonProperty("fields")] public IList<RocketField> Fields { get; set; }
    }
}