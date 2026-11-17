using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JftwGo.Core.Logging;
using JftwGo.Core.Models;
using JftwGo.Core.Packaging;
using JftwGo.Core.Usb;

namespace JftwGo.Installer
{
    /// <summary>
    /// U 盘资源部署器（非破坏性，不格式化、不清盘）。
    /// 目录结构（类似 Minecraft 的 .minecraft/version）：
    ///   U:\jftw_Go_Assets\version\&lt;整合包标识&gt;\   ← 每个整合包一个文件夹，内含 start1.exe
    ///   U:\jftw Go.lnk                                  ← 根目录快捷方式，指向本次写入包的 start1.exe
    ///   U:\jftw Go.bat                                  ← 根目录 bat，启动 start1.exe
    /// 目标文件夹 jftw_Go_Assets 已存在时视为"被占用"，由调用方提示用户。
    /// </summary>
    public class UsbWriter
    {
        public const string AssetsFolderName = "jftw_Go_Assets";
        public const string VersionFolderName = "version";
        public const string LauncherExeName = "start1.exe";
        public const string ShortcutName = "jftw Go.lnk";
        public const string BatName = "jftw Go.bat";

        private readonly ILogger _logger;

        public UsbWriter(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>检查 jftw_Go_Assets 是否已被占用。</summary>
        public static bool IsAssetsFolderOccupied(UsbDeviceInfo usb)
        {
            string path = Path.Combine(ToRoot(usb.DriveLetter), AssetsFolderName);
            return Directory.Exists(path);
        }

        /// <summary>整合包在 version 下的文件夹名（用 Id 做稳定标识，非法字符替换为下划线）。</summary>
        public static string GetPackageFolderName(ResourceItem item)
        {
            string id = item != null && !string.IsNullOrWhiteSpace(item.Id) ? item.Id : "package";
            foreach (char c in Path.GetInvalidFileNameChars()) id = id.Replace(c, '_');
            return id;
        }

        /// <summary>
        /// 部署一个整合包到 U 盘（不清除其它内容）。
        /// 返回部署结果，含 start1.exe 实际路径与快捷方式/bat 是否创建成功。
        /// </summary>
        public async Task<DeployResult> DeployAsync(string packageFile, UsbDeviceInfo usb,
            ResourceItem item, IProgress<int> progress, CancellationToken ct)
        {
            var result = new DeployResult();
            string root = ToRoot(usb.DriveLetter);
            string versionDir = Path.Combine(root, AssetsFolderName, VersionFolderName);
            string packageDir = Path.Combine(versionDir, GetPackageFolderName(item));

            Directory.CreateDirectory(versionDir);
            Directory.CreateDirectory(packageDir);
            _logger.Info("部署目标目录：" + packageDir);

            // 释放资源包到"该整合包自己的文件夹"
            if (ArchiveExtractor.IsZip(packageFile))
            {
                await ArchiveExtractor.ExtractZipAsync(packageFile, packageDir, progress, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                ArchiveExtractor.CopyFileToDir(packageFile, packageDir);
                if (progress != null) progress.Report(100);
            }

            // 查找 start1.exe（可能在包内子目录）
            string exe = FindLauncher(packageDir);
            result.LauncherPath = exe;
            if (string.IsNullOrEmpty(exe))
            {
                result.LauncherFound = false;
                result.Warning = "未在整合包中找到 " + LauncherExeName + "，资源已解压但未创建快捷方式。";
                _logger.Warn(result.Warning);
                result.Success = true; // 资源已就位，仅快捷方式缺失
                return result;
            }

            result.LauncherFound = true;
            // 根目录快捷方式与 bat
            result.ShortcutCreated = CreateShortcut(Path.Combine(root, ShortcutName), exe, packageDir);
            result.BatCreated = CreateBat(Path.Combine(root, BatName), exe, packageDir);
            result.Success = true;
            _logger.Info("部署完成，启动程序：" + exe);
            return result;
        }

        /// <summary>估算可用空间是否足够（含 2% 余量）。</summary>
        public static bool HasEnoughSpace(UsbDeviceInfo usb, long? requiredBytes)
        {
            if (!requiredBytes.HasValue || requiredBytes.Value <= 0) return true;
            long need = (long)(requiredBytes.Value * 1.02);
            return usb.FreeBytes >= need;
        }

        private static string FindLauncher(string packageDir)
        {
            try
            {
                string direct = Path.Combine(packageDir, LauncherExeName);
                if (File.Exists(direct)) return direct;
                string[] found = Directory.GetFiles(packageDir, LauncherExeName, SearchOption.AllDirectories);
                return found.Length > 0 ? found[0] : null;
            }
            catch { return null; }
        }

        /// <summary>用 WScript.Shell COM 动态创建快捷方式，无需添加 COM 引用。</summary>
        private bool CreateShortcut(string lnkPath, string target, string workingDir)
        {
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) throw new InvalidOperationException("系统未提供 WScript.Shell");
                object shell = Activator.CreateInstance(t);
                object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null,
                    shell, new object[] { lnkPath });
                Type scType = sc.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workingDir });
                scType.InvokeMember("Arguments", BindingFlags.SetProperty, null, sc, new object[] { "" });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, sc,
                    new object[] { "启动 jftw Go" });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
                _logger.Info("已创建快捷方式：" + lnkPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn("创建快捷方式失败：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 生成 bat：用 %~dp0 定位 U 盘根目录，相对路径调用整合包内的 start1.exe，
        /// 这样即使 U 盘盘符变化也能正常启动。
        /// </summary>
        private bool CreateBat(string batPath, string launcherFullPath, string packageDir)
        {
            try
            {
                string root = Path.GetPathRoot(launcherFullPath);
                string rel = launcherFullPath.Substring(root.Length); // 相对 U 盘根的路径
                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("chcp 65001 >nul");
                sb.AppendLine("rem jftw Go 启动脚本（相对路径，盘符变化仍可用）");
                sb.AppendLine("start \"\" \"%~dp0" + rel + "\"");
                File.WriteAllText(batPath, sb.ToString(), Encoding.UTF8);
                _logger.Info("已创建启动脚本：" + batPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn("创建 bat 失败：" + ex.Message);
                return false;
            }
        }

        private static string ToRoot(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                throw new ArgumentException("盘符为空");
            string s = driveLetter.Trim();
            if (!s.EndsWith("\\")) s += "\\";
            return s;
        }

        public class DeployResult
        {
            public bool Success { get; set; }
            public bool LauncherFound { get; set; }
            public bool ShortcutCreated { get; set; }
            public bool BatCreated { get; set; }
            public string LauncherPath { get; set; }
            public string Warning { get; set; }
        }
    }
}
