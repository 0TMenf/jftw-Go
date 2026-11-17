using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace JftwGo.Core.Usb
{
    /// <summary>
    /// 通过 WMI 枚举可移动磁盘（DriveType=2），读取容量/文件系统/可用空间/型号。
    /// 同时提供安全弹出（CM_Disable_DevNode 之外，采用 WM_DEVICECHANGE 常用做法由上层调用 shell 弹出）。
    /// </summary>
    public static class UsbScanner
    {
        public static List<UsbDeviceInfo> Scan()
        {
            var list = new List<UsbDeviceInfo>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace, Model, PNPDeviceID " +
                    "FROM Win32_LogicalDisk WHERE DriveType = 2"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        using (mo)
                        {
                            var info = new UsbDeviceInfo();
                            info.DriveLetter = GetStr(mo, "DeviceID");
                            info.VolumeLabel = GetStr(mo, "VolumeName");
                            info.FileSystem = GetStr(mo, "FileSystem");
                            info.TotalBytes = GetLong(mo, "Size");
                            info.FreeBytes = GetLong(mo, "FreeSpace");
                            info.DeviceId = GetStr(mo, "PNPDeviceID");
                            info.Model = ResolveModel(info.DeviceId, GetStr(mo, "Model"));
                            list.Add(info);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // WMI 不可用时回退到 DriveInfo
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Removable) continue;
                    list.Add(new UsbDeviceInfo
                    {
                        DriveLetter = d.Name.TrimEnd('\\'),
                        VolumeLabel = d.VolumeLabel,
                        FileSystem = d.DriveFormat,
                        TotalBytes = d.TotalSize,
                        FreeBytes = d.AvailableFreeSpace,
                        Model = "可移动磁盘"
                    });
                }
            }
            return list;
        }

        /// <summary>校验盘符是否仍为可移动磁盘（写入前二次确认）。</summary>
        public static bool IsStillRemovable(string driveLetter)
        {
            try
            {
                string name = driveLetter;
                if (!name.EndsWith("\\")) name += "\\";
                var d = new DriveInfo(name);
                return d.IsReady && d.DriveType == DriveType.Removable;
            }
            catch { return false; }
        }

        private static string ResolveModel(string pnpId, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
            if (string.IsNullOrWhiteSpace(pnpId)) return "USB 设备";
            try
            {
                // PNPDeviceID 形如 USBSTOR\DISK&VEN_SANDISK&PROD_CRUIZER&REV...
                string[] parts = pnpId.Split('\\');
                if (parts.Length > 1)
                {
                    string[] toks = parts[1].Split('&', '#');
                    foreach (string t in toks)
                        if (t.StartsWith("PROD_", StringComparison.OrdinalIgnoreCase))
                            return t.Substring(5);
                }
            }
            catch { }
            return "USB 设备";
        }

        private static string GetStr(ManagementObject mo, string name)
        {
            try { object v = mo[name]; return v == null ? string.Empty : v.ToString(); }
            catch { return string.Empty; }
        }

        private static long GetLong(ManagementObject mo, string name)
        {
            try { object v = mo[name]; return v == null ? 0 : Convert.ToInt64(v); }
            catch { return 0; }
        }
    }
}
