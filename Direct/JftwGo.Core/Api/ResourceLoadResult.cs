using System;
using JftwGo.Core.Models;

namespace JftwGo.Core.Api
{
    /// <summary>加载状态枚举，驱动 UI 渲染（加载中/成功/空/失败）。</summary>
    public enum ResourceLoadState
    {
        Loading,
        Loaded,
        Empty,
        Failed
    }

    /// <summary>一次资源加载的结果封装。</summary>
    public class ResourceLoadResult
    {
        public ResourceLoadState State { get; set; }
        public ResourceCatalog Catalog { get; set; }
        public string ErrorMessage { get; set; }
        public bool FromCache { get; set; }

        public bool IsSuccess { get { return State == ResourceLoadState.Loaded; } }

        public static ResourceLoadResult Loading()
        {
            return new ResourceLoadResult { State = ResourceLoadState.Loading };
        }

        public static ResourceLoadResult Success(ResourceCatalog catalog, bool fromCache)
        {
            return new ResourceLoadResult
            {
                State = ResourceLoadState.Loaded,
                Catalog = catalog,
                FromCache = fromCache
            };
        }

        public static ResourceLoadResult Empty(ResourceCatalog catalog)
        {
            return new ResourceLoadResult { State = ResourceLoadState.Empty, Catalog = catalog };
        }

        public static ResourceLoadResult Failed(string message)
        {
            return new ResourceLoadResult { State = ResourceLoadState.Failed, ErrorMessage = message };
        }
    }
}
