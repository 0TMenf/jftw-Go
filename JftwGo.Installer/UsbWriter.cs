using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JftwGo.Core.Logging;
using JftwGo.Core.Packaging;
using JftwGo.Core.Usb;

namespace JftwGo.Installer
{
    /// <summary>
    /// U 盘写入器：清空目标盘 → 释放资源包到盘根 → 写入标记文件。
    /// 说明：启动引导文件由资源包自身提供（包内若含 boot 目录/引导文件，会一并释放到盘根），
    /// 本类负责搬运与校验，不伪造引导扇区。
    /// </summary>
    public class UsbWriter
    {
        private readonly ILogger _logger;

        public UsbWriter(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>清空 U 盘根目录下的所有内容（保留盘符本身）。</summary>
        public Task ClearAsync(UsbDeviceInfo usb, IProgress<int> progress, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                string root = ToRoot(usb.DriveLetter);
                _logger.Info("开始清空 U 盘：" + root);

                var entries = new DirectoryInfo(root).GetFileSystemInfos();
                int total = Math.Max(1, entries.Length);
                int done = 0;
                foreach (var entry in entries)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var dir = entry as DirectoryInfo;
                        if (dir != null) dir.Delete(true);
                        else entry.Delete();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("删除失败（已跳过）：" + entry.FullName + " · " + ex.Message);
                    }
                    done++;
                    if (progress != null) progress.Report((int)(done * 100L / total));
                }
                _logger.Info("U 盘清空完成。");
            }, ct);
        }

        /// <summary>释放资源包到 U 盘根目录。</summary>
        public async Task WriteAsync(string packageFile, UsbDeviceInfo usb,
            IProgress<int> progress, CancellationToken ct)
        {
            string root = ToRoot(usb.DriveLetter);
            if (ArchiveExtractor.IsZip(packageFile))
            {
                _logger.Info("正在解压资源包到 U 盘根目录…");
                await ArchiveExtractor.ExtractZipAsync(packageFile, root, progress, ct).ConfigureAwait(false);
            }
            else
            {
                _logger.Info("资源包不是 zip，按单文件复制到 U 盘根目录。");
                ArchiveExtractor.CopyFileToDir(packageFile, root);
                if (progress != null) progress.Report(100);
            }
        }

        /// <summary>写入制作标记，便于识别该盘由 jftw Go 制作。</summary>
        public void WriteMarker(UsbDeviceInfo usb, string resourceName, string version)
        {
            try
            {
                string root = ToRoot(usb.DriveLetter);
                string content =
                    "jftw Go portable media" + Environment.NewLine +
                    "resource: " + resourceName + Environment.NewLine +
                    "version: " + (version ?? "-") + Environment.NewLine +
                    "created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine;
                File.WriteAllText(Path.Combine(root, "jftwgo-media.txt"), content,
                    System.Text.Encoding.UTF8);
                _logger.Info("已写入制作标记 jftwgo-media.txt。");
            }
            catch (Exception ex)
            {
                _logger.Warn("写入标记失败：" + ex.Message);
            }
        }

        /// <summary>估算可用空间是否足够。</summary>
        public static bool HasEnoughSpace(UsbDeviceInfo usb, long? requiredBytes)
        {
            if (!requiredBytes.HasValue || requiredBytes.Value <= 0) return true;
            // 预留 2% 余量
            long need = (long)(requiredBytes.Value * 1.02);
            return usb.FreeBytes >= need;
        }

        private static string ToRoot(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                throw new ArgumentException("盘符为空");
            string s = driveLetter.Trim();
            if (!s.EndsWith("\\")) s += "\\";
            return s;
        }
    }
}
