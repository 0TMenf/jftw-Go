using Newtonsoft.Json;

namespace JftwGo.Core.Models
{
    /// <summary>校验和信息：{ "algorithm": "SHA256", "value": "..." }</summary>
    public class ChecksumInfo
    {
        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>算法是否可用（当前支持 SHA256 / SHA1 / MD5）。</summary>
        public bool IsSupported
        {
            get
            {
                if (string.IsNullOrEmpty(Algorithm)) return false;
                string a = Algorithm.Trim().ToUpperInvariant();
                return a == "SHA256" || a == "SHA-256" || a == "SHA1" || a == "SHA-1" || a == "MD5";
            }
        }
    }
}
