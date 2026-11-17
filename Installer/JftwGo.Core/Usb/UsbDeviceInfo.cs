using System;

using JftwGo.Core.Localization;

namespace JftwGo.Core.Usb
{
    /// <summary>可移动磁盘信息。</summary>
    public class UsbDeviceInfo
    {
        public string DriveLetter { get; set; }       // 例如 "E:"
        public string VolumeLabel { get; set; }
        public string FileSystem { get; set; }
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
        public string Model { get; set; }             // 设备型号
        public string DeviceId { get; set; }

        public string DisplayName
        {
            get
            {
                string label = string.IsNullOrWhiteSpace(VolumeLabel) ? L.Get("usb.removable") : VolumeLabel;
                return string.Format("{0} ({1})  {2}", label, DriveLetter, Model);
            }
        }

        public string CapacityText { get { return FormatSize(TotalBytes); } }
        public string FreeText { get { return L.Format("usb.free", FormatSize(FreeBytes)); } }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024L * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }
    }
}
