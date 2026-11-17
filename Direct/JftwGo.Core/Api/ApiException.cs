using System;

namespace JftwGo.Core.Api
{
    /// <summary>网络请求失败。</summary>
    public class ApiNetworkException : Exception
    {
        public ApiNetworkException(string message) : base(message) { }
        public ApiNetworkException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>JSON 解析失败。</summary>
    public class ResourceParseException : Exception
    {
        public ResourceParseException(string message) : base(message) { }
        public ResourceParseException(string message, Exception inner) : base(message, inner) { }
    }
}
