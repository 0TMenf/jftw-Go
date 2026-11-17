using Newtonsoft.Json;

namespace JftwGo.Core.Models
{
    /// <summary>API 顶层响应：schemaVersion / generatedAt / resources。</summary>
    public class ResourceCatalog
    {
        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("generatedAt")]
        public string GeneratedAtRaw { get; set; }

        [JsonProperty("resources")]
        public ResourceContainer Resources { get; set; }

        public ResourceCatalog()
        {
            Resources = new ResourceContainer();
        }
    }
}
