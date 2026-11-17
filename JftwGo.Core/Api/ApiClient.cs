using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JftwGo.Core.Models;
using JftwGo.Core.Logging;
using Newtonsoft.Json;

namespace JftwGo.Core.Api
{
    /// <summary>
    /// 资源列表 API 客户端。
    /// 职责：GET + 超时 + 重试 + 本地缓存 + JSON 解析容错 + 数据归一化 + 相对图标 URL 绝对化。
    /// 不依赖任何 UI，可被两个子程序共享。
    /// </summary>
    public class ApiClient
    {
        private readonly ApiOptions _options;
        private readonly ILogger _logger;

        public ApiClient(ApiOptions options, ILogger logger)
        {
            _options = options ?? ApiOptions.Default;
            _logger = logger ?? NullLogger.Instance;
        }

        public ApiClient(ILogger logger) : this(ApiOptions.Default, logger) { }

        /// <summary>
        /// 异步加载资源目录。优先网络；网络失败时回退本地缓存并标注 FromCache。
        /// 返回统一的 ResourceLoadResult，调用方据此渲染加载中/成功/空/失败状态。
        /// </summary>
        public async Task<ResourceLoadResult> LoadAsync(CancellationToken ct)
        {
            try
            {
                string json = await GetWithRetryAsync(ct).ConfigureAwait(false);
                ResourceCatalog catalog = Parse(json);
                Normalize(catalog);
                return HasAnyResource(catalog)
                    ? ResourceLoadResult.Success(catalog, false)
                    : ResourceLoadResult.Empty(catalog);
            }
            catch (OperationCanceledException)
            {
                throw; // 取消交由上层处理
            }
            catch (Exception ex)
            {
                _logger.Warn("获取在线资源失败：" + ex.Message + "，尝试读取本地缓存。");
                ResourceCatalog cached;
                if (TryLoadCache(out cached))
                {
                    Normalize(cached);
                    return HasAnyResource(cached)
                        ? ResourceLoadResult.Success(cached, true)
                        : ResourceLoadResult.Empty(cached);
                }
                return ResourceLoadResult.Failed(ex.Message);
            }
        }

        // ---- GET + 重试 + 超时 ----
        private async Task<string> GetWithRetryAsync(CancellationToken ct)
        {
            Exception last = null;
            for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using (var handler = new HttpClientHandler())
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = _options.Timeout;
                        client.DefaultRequestHeaders.Add("User-Agent", "jftwGo/1.0");
                        var response = await client.GetAsync(_options.Endpoint, ct).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(json))
                            throw new ApiNetworkException("服务器返回空内容");
                        _logger.Info("资源列表获取成功（第 " + attempt + " 次尝试）。");
                        SaveCache(json);
                        return json;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _logger.Warn("第 " + attempt + "/" + _options.MaxRetries + " 次请求失败：" + ex.Message);
                    if (attempt < _options.MaxRetries)
                    {
                        int delayMs = (int)(_options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                        await Task.Delay(delayMs, ct).ConfigureAwait(false);
                    }
                }
            }
            throw new ApiNetworkException("资源列表获取失败，已重试 " + _options.MaxRetries + " 次：" +
                                          (last == null ? "未知错误" : last.Message), last);
        }

        // ---- JSON 解析 ----
        private ResourceCatalog Parse(string json)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                ResourceCatalog catalog = JsonConvert.DeserializeObject<ResourceCatalog>(json, settings);
                if (catalog == null)
                    throw new ResourceParseException("JSON 顶层为空");
                if (catalog.Resources == null)
                    catalog.Resources = new ResourceContainer();
                return catalog;
            }
            catch (ResourceParseException) { throw; }
            catch (JsonException ex)
            {
                throw new ResourceParseException("资源列表 JSON 解析失败：" + ex.Message, ex);
            }
        }

        // ---- 归一化：过滤非法项 + 相对图标 URL 绝对化 ----
        private void Normalize(ResourceCatalog catalog)
        {
            if (catalog == null || catalog.Resources == null) return;
            var r = catalog.Resources;

            r.Official = r.Official ?? new OfficialChannels();
            NormalizeItem(r.Official.Stable, ChannelKind.Stable);
            NormalizeItem(r.Official.Preview, ChannelKind.Preview);
            NormalizeItem(r.Official.Dev, ChannelKind.Dev);

            r.Historical = NormalizeList(r.Historical);
            r.Community = NormalizeList(r.Community, ChannelKind.Community);
            r.Games = NormalizeList(r.Games, ChannelKind.Game);
        }

        private List<ResourceItem> NormalizeList(List<ResourceItem> list, ChannelKind? forceChannel = null)
        {
            var result = new List<ResourceItem>();
            if (list == null) return result;
            foreach (var item in list)
            {
                if (item == null || !item.IsValid) continue;
                if (forceChannel.HasValue)
                    item.ChannelRaw = forceChannel.Value.ToString().ToLowerInvariant();
                NormalizeItem(item, null);
                result.Add(item);
            }
            return result;
        }

        private void NormalizeItem(ResourceItem item, ChannelKind? forceChannel)
        {
            if (item == null) return;
            if (forceChannel.HasValue && string.IsNullOrWhiteSpace(item.ChannelRaw))
                item.ChannelRaw = forceChannel.Value.ToString().ToLowerInvariant();
            item.IconUrl = ResolveIconUrl(item.IconUrl);
        }

        /// <summary>相对路径基于 API origin 解析为绝对 URL；绝对 URL 原样返回；空返回 null。</summary>
        public string ResolveIconUrl(string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(iconUrl)) return null;
            string s = iconUrl.Trim();
            Uri absolute;
            if (Uri.TryCreate(s, UriKind.Absolute, out absolute))
                return absolute.AbsoluteUri;
            Uri origin;
            Uri relative;
            if (Uri.TryCreate(_options.ApiOrigin, UriKind.Absolute, out origin) &&
                Uri.TryCreate(s, UriKind.Relative, out relative))
                return new Uri(origin, relative).AbsoluteUri;
            return s;
        }

        private static bool HasAnyResource(ResourceCatalog catalog)
        {
            if (catalog == null || catalog.Resources == null) return false;
            var r = catalog.Resources;
            return IsValidItem(r.Official != null ? r.Official.Stable : null)
                || IsValidItem(r.Official != null ? r.Official.Preview : null)
                || IsValidItem(r.Official != null ? r.Official.Dev : null)
                || (r.Historical != null && r.Historical.Count > 0)
                || (r.Community != null && r.Community.Count > 0)
                || (r.Games != null && r.Games.Count > 0);
        }

        private static bool IsValidItem(ResourceItem item)
        {
            return item != null && item.IsValid;
        }

        // ---- 本地缓存 ----
        private void SaveCache(string json)
        {
            try
            {
                File.WriteAllText(AppPaths.CatalogCacheFile, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.Warn("写入资源缓存失败：" + ex.Message);
            }
        }

        private bool TryLoadCache(out ResourceCatalog catalog)
        {
            catalog = null;
            try
            {
                string file = AppPaths.CatalogCacheFile;
                if (!File.Exists(file)) return false;
                string json = File.ReadAllText(file, System.Text.Encoding.UTF8);
                catalog = Parse(json);
                return catalog != null;
            }
            catch (Exception ex)
            {
                _logger.Warn("读取资源缓存失败：" + ex.Message);
                return false;
            }
        }
    }
}
