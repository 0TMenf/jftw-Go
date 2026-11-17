using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JftwGo.Core.Logging;
using JftwGo.Core.Models;
using JftwGo.Core.Verify;

namespace JftwGo.Core.Download
{
    /// <summary>下载结果。</summary>
    public class DownloadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public ChecksumVerifier.VerifyResult Verification { get; set; }
        public string ErrorMessage { get; set; }
        public long BytesTotal { get; set; }
    }

    /// <summary>
    /// 通用下载器：流式写盘 + 进度回调 + 取消 + 失败重试 + 日志 + 可选 checksum 校验。
    /// 校验失败视为下载失败并删除文件。两个子程序共享。
    /// </summary>
    public class Downloader
    {
        private readonly ILogger _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _timeout;

        public Downloader(ILogger logger) : this(logger, 3, TimeSpan.FromSeconds(30)) { }

        public Downloader(ILogger logger, int maxRetries, TimeSpan timeout)
        {
            _logger = logger ?? NullLogger.Instance;
            _maxRetries = Math.Max(1, maxRetries);
            _timeout = timeout;
        }

        /// <summary>
        /// 下载 ResourceItem 到目标路径。progress 可为 null。ct 用于取消。
        /// </summary>
        public async Task<DownloadResult> DownloadAsync(
            ResourceItem item,
            string targetPath,
            IProgress<DownloadProgressInfo> progress,
            CancellationToken ct)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.DownloadUrl))
                return Fail("资源下载地址为空");

            string dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string temp = targetPath + ".part";

            Exception last = null;
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    Report(progress, 0, item.FileSize, 0, "准备下载（第 " + attempt + " 次）");
                    _logger.Info("开始下载：" + item.Name + "（第 " + attempt + " 次）");

                    await StreamDownloadAsync(item.DownloadUrl, temp, item.FileSize, progress, ct)
                        .ConfigureAwait(false);

                    ct.ThrowIfCancellationRequested();

                    var verify = ChecksumVerifier.Verify(temp, item.Checksum);
                    if (verify.WasRequested && !verify.Success)
                    {
                        _logger.Error(verify.ErrorMessage);
                        TryDelete(temp);
                        last = new Exception(verify.ErrorMessage);
                        if (attempt < _maxRetries)
                        {
                            await Task.Delay(500 * attempt, ct).ConfigureAwait(false);
                            continue;
                        }
                        return new DownloadResult
                        {
                            Success = false,
                            Verification = verify,
                            ErrorMessage = verify.ErrorMessage
                        };
                    }

                    if (File.Exists(targetPath)) File.Delete(targetPath);
                    File.Move(temp, targetPath);
                    _logger.Info("下载完成：" + targetPath);

                    var result = new DownloadResult
                    {
                        Success = true,
                        FilePath = targetPath,
                        Verification = verify,
                        BytesTotal = new FileInfo(targetPath).Length
                    };
                    Report(progress, 100, result.BytesTotal, 0, verify.WasRequested ? "下载完成，校验通过" : "下载完成");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    TryDelete(temp);
                    throw;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _logger.Warn("下载失败（第 " + attempt + "/" + _maxRetries + " 次）：" + ex.Message);
                    TryDelete(temp);
                    if (attempt < _maxRetries)
                        await Task.Delay(600 * attempt, ct).ConfigureAwait(false);
                }
            }
            return Fail("下载失败：" + (last == null ? "未知错误" : last.Message));
        }

        private async Task StreamDownloadAsync(
            string url, string temp, long? expectedTotal,
            IProgress<DownloadProgressInfo> progress, CancellationToken ct)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = Timeout.InfiniteTimeSpan; // 用 CancellationToken 统一控制取消与超时
                client.DefaultRequestHeaders.Add("User-Agent", "jftwGo/1.0");
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    long? total = response.Content.Headers.ContentLength;
                    if (!total.HasValue && expectedTotal.HasValue) total = expectedTotal;

                    using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None,
                        81920, true))
                    {
                        var buffer = new byte[81920];
                        long received = 0;
                        var sw = Stopwatch.StartNew();
                        int lastPercent = -1;
                        while (true)
                        {
                            ct.ThrowIfCancellationRequested();
                            int read = await source.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                            if (read == 0) break;
                            await fs.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                            received += read;

                            int percent = 0;
                            if (total.HasValue && total.Value > 0)
                                percent = (int)Math.Min(100, received * 100L / total.Value);
                            if (percent != lastPercent || received == total)
                            {
                                double speed = sw.Elapsed.TotalSeconds > 0.1
                                    ? received / sw.Elapsed.TotalSeconds : 0;
                                Report(progress, percent, total, speed, null);
                                lastPercent = percent;
                            }
                        }
                        await fs.FlushAsync(ct).ConfigureAwait(false);
                    }
                }
            }
        }

        private static void Report(IProgress<DownloadProgressInfo> progress, int percent,
            long? total, double speed, string status)
        {
            if (progress == null) return;
            var p = new DownloadProgressInfo
            {
                Percent = percent,
                TotalBytes = total,
                BytesReceived = total.HasValue ? (long)(total.Value * percent / 100.0) : 0,
                SpeedBytesPerSec = speed,
                StatusText = status
            };
            progress.Report(p);
        }

        private static DownloadResult Fail(string message)
        {
            return new DownloadResult { Success = false, ErrorMessage = message };
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
