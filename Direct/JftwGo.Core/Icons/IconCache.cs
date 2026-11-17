using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JftwGo.Core.Logging;

namespace JftwGo.Core.Icons
{
    /// <summary>
    /// 图标两级缓存 + 异步加载。
    /// 一级：内存字典（Bitmap）；二级：磁盘目录。
    /// 加载不阻塞调用方；失败/空/超时返回 null，由 UI 显示占位图。
    /// </summary>
    public class IconCache
    {
        private readonly Dictionary<string, Image> _memory = new Dictionary<string, Image>();
        private readonly object _sync = new object();
        private readonly ILogger _logger;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private static Image _placeholder;

        public IconCache(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>统一默认占位图（内存生成，避免额外资源文件）。</summary>
        public static Image Placeholder
        {
            get
            {
                if (_placeholder == null)
                {
                    var bmp = new Bitmap(48, 48, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(230, 230, 230));
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var pen = new Pen(Color.FromArgb(170, 170, 170), 2))
                            g.DrawRectangle(pen, 8, 8, 31, 31);
                        using (var pen = new Pen(Color.FromArgb(170, 170, 170), 2))
                        {
                            g.DrawLine(pen, 8, 39, 22, 24);
                            g.DrawLine(pen, 22, 24, 30, 31);
                            g.DrawLine(pen, 30, 31, 39, 22);
                        }
                        using (var brush = new SolidBrush(Color.FromArgb(170, 170, 170)))
                            g.FillEllipse(brush, 27, 14, 6, 6);
                    }
                    _placeholder = bmp;
                }
                return _placeholder;
            }
        }

        /// <summary>
        /// 异步获取图标。命中缓存立即从内存/磁盘返回；否则后台下载。
        /// 返回 null 表示失败（调用方用占位图）。
        /// </summary>
        public async Task<Image> GetAsync(string url, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            string key = CacheKey(url);
            Image cached = TryGetMemory(key);
            if (cached != null) return cached;

            string diskPath = Path.Combine(AppPaths.IconCacheDir, key + ".img");
            if (File.Exists(diskPath))
            {
                Image fromDisk = TryLoadImage(diskPath);
                if (fromDisk != null)
                {
                    lock (_sync) { _memory[key] = fromDisk; }
                    return fromDisk;
                }
            }

            try
            {
                byte[] bytes;
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = _timeout;
                    client.DefaultRequestHeaders.Add("User-Agent", "jftwGo/1.0");
                    var resp = await client.GetAsync(url, ct).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
                Image img = BytesToImage(bytes);
                if (img == null) return null;
                try { File.WriteAllBytes(diskPath, bytes); }
                catch (Exception ex) { _logger.Warn("图标写缓存失败：" + ex.Message); }
                lock (_sync) { _memory[key] = img; }
                return img;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Warn("图标加载失败 " + url + "：" + ex.Message);
                return null;
            }
        }

        /// <summary>等比缩放并居中绘制到目标尺寸的位图，供卡片使用。</summary>
        public static Image MakeThumbnail(Image source, int size)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (source != null)
                {
                    int w = source.Width, h = source.Height;
                    if (w <= 0 || h <= 0) { w = size; h = size; }
                    double scale = Math.Min((double)size / w, (double)size / h);
                    int dw = Math.Max(1, (int)Math.Round(w * scale));
                    int dh = Math.Max(1, (int)Math.Round(h * scale));
                    int dx = (size - dw) / 2, dy = (size - dh) / 2;
                    g.DrawImage(source, dx, dy, dw, dh);
                }
                else
                {
                    g.DrawImage(Placeholder, 0, 0, size, size);
                }
            }
            return bmp;
        }

        private Image TryGetMemory(string key)
        {
            lock (_sync)
            {
                Image img;
                return _memory.TryGetValue(key, out img) ? img : null;
            }
        }

        private static Image TryLoadImage(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    Image img = Image.FromStream(fs, true, false);
                    return new Bitmap(img); // 脱离流
                }
            }
            catch { return null; }
        }

        private static Image BytesToImage(byte[] bytes)
        {
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    Image img = Image.FromStream(ms, true, false);
                    return new Bitmap(img);
                }
            }
            catch { return null; }
        }

        private static string CacheKey(string url)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
                var sb = new StringBuilder();
                for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
