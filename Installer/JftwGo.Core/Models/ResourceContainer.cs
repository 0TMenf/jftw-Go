using System.Collections.Generic;
using Newtonsoft.Json;

namespace JftwGo.Core.Models
{
    /// <summary>resources 容器：官方三通道 + 三个资源列表。</summary>
    public class ResourceContainer
    {
        [JsonProperty("official")]
        public OfficialChannels Official { get; set; }

        [JsonProperty("historical")]
        public List<ResourceItem> Historical { get; set; }

        [JsonProperty("community")]
        public List<ResourceItem> Community { get; set; }

        [JsonProperty("games")]
        public List<ResourceItem> Games { get; set; }

        public ResourceContainer()
        {
            Official = new OfficialChannels();
            Historical = new List<ResourceItem>();
            Community = new List<ResourceItem>();
            Games = new List<ResourceItem>();
        }
    }
}
