using System;
using System.Configuration;

namespace JftwGo.Core.Api
{
    /// <summary>API 客户端配置。Endpoint/ApiOrigin 从 exe.config 的 appSettings 读取，未配置则用默认值。</summary>
    public class ApiOptions
    {
        /// <summary>资源列表接口地址（可精确到某个 JSON 文件 URL）。</summary>
        public string Endpoint { get; set; }

        /// <summary>单次请求超时。</summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>最大尝试次数（含首次）。</summary>
        public int MaxRetries { get; set; }

        /// <summary>重试基础退避时间，实际为其指数倍。</summary>
        public TimeSpan RetryBaseDelay { get; set; }

        /// <summary>API origin，用于解析相对图标 URL。</summary>
        public string ApiOrigin { get; set; }

        public ApiOptions()
        {
            Endpoint = Read("ApiEndpoint", "https://jftw.pinkoc.top/api/jftwGo/");
            Timeout = TimeSpan.FromSeconds(15);
            MaxRetries = 3;
            RetryBaseDelay = TimeSpan.FromMilliseconds(600);
            ApiOrigin = Read("ApiOrigin", "https://jftw.pinkoc.top");
        }

        private static string Read(string key, string fallback)
        {
            try
            {
                string v = ConfigurationManager.AppSettings[key];
                return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
            }
            catch { return fallback; }
        }

        public static ApiOptions Default { get { return new ApiOptions(); } }
    }
}
