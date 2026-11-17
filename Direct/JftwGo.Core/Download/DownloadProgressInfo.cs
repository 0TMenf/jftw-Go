namespace JftwGo.Core.Download
{
    /// <summary>下载进度快照。Percent 为 0-100。</summary>
    public class DownloadProgressInfo
    {
        public long BytesReceived { get; set; }
        public long? TotalBytes { get; set; }
        public int Percent { get; set; }
        public double SpeedBytesPerSec { get; set; }
        public string StatusText { get; set; }

        public string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }
    }
}
