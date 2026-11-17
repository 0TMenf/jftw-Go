using Newtonsoft.Json;

namespace JftwGo.Core.Models
{
    /// <summary>官方整合包的三个通道，每个值为一个 ResourceItem。</summary>
    public class OfficialChannels
    {
        [JsonProperty("stable")]
        public ResourceItem Stable { get; set; }

        [JsonProperty("preview")]
        public ResourceItem Preview { get; set; }

        [JsonProperty("dev")]
        public ResourceItem Dev { get; set; }

        public OfficialChannels()
        {
            Stable = new ResourceItem();
            Preview = new ResourceItem();
            Dev = new ResourceItem();
        }
    }
}
