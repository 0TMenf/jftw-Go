using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JftwGo.Core.Models
{
    /// <summary>
    /// 统一资源模型。对应 JSON 契约中的 ResourceItem。
    /// 可选字段全部给出容错默认值；未知字段由 JSON 库自动忽略。
    /// </summary>
    public class ResourceItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("channel")]
        public string ChannelRaw { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("fileSize")]
        public long? FileSize { get; set; }

        [JsonProperty("checksum")]
        public ChecksumInfo Checksum { get; set; }

        [JsonProperty("updatedAt")]
        public string UpdatedAtRaw { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("screenshots")]
        public List<string> Screenshots { get; set; }

        // ---- 归一化后的派生属性（不参与反序列化） ----

        [JsonIgnore]
        public ChannelKind Channel
        {
            get { return ChannelKindParser.Parse(ChannelRaw); }
        }

        [JsonIgnore]
        public DateTime? UpdatedAt
        {
            get { return Iso8601.TryParse(UpdatedAtRaw); }
        }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Id)
                    && !string.IsNullOrWhiteSpace(Name)
                    && !string.IsNullOrWhiteSpace(DownloadUrl);
            }
        }

        public ResourceItem()
        {
            Tags = new List<string>();
            Screenshots = new List<string>();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Version) ? Name : (Name + " " + Version);
        }
    }

    /// <summary>channel 字符串 → 枚举的容错解析。</summary>
    internal static class ChannelKindParser
    {
        public static ChannelKind Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ChannelKind.Unknown;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "stable": return ChannelKind.Stable;
                case "preview": return ChannelKind.Preview;
                case "dev":
                case "develop":
                case "development": return ChannelKind.Dev;
                case "community": return ChannelKind.Community;
                case "game":
                case "games": return ChannelKind.Game;
                default: return ChannelKind.Unknown;
            }
        }
    }

    /// <summary>ISO 8601 时间容错解析，失败返回 null。</summary>
    internal static class Iso8601
    {
        public static DateTime? TryParse(string s)
        {
            DateTime dt;
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out dt))
                return dt;
            return null;
        }
    }
}
