using System;

namespace JftwGo.Core.Api
{
    /// <summary>API 客户端配置。</summary>
    public class ApiOptions
    {
        /// <summary>资源列表接口地址。</summary>
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
            Endpoint = "https://jftw.pinkoc.top/api/jftwGo/";
            Timeout = TimeSpan.FromSeconds(15);
            MaxRetries = 3;
            RetryBaseDelay = TimeSpan.FromMilliseconds(600);
            ApiOrigin = "https://jftw.pinkoc.top";
        }

        public static ApiOptions Default { get { return new ApiOptions(); } }
    }
}
