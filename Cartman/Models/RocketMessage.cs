using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cartman.Processor.Models
{
    public class RocketMessage
    {
        public RocketMessage()
        {
            Attachments = new List<RocketAttachment>();
        }
        [JsonProperty("username")]
        public string UserName { get; set; }
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("attachments")]
        public IList<RocketAttachment> Attachments { get; set; }
    }
}
