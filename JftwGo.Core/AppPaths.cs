using System;
using System.IO;

namespace JftwGo.Core
{
    /// <summary>
    /// 集中解析本软件的本机目录：缓存、日志、临时、下载。
    /// 全部位于 %LocalAppData%\jftwGo 下，不写程序目录，兼容标准用户权限。
    /// </summary>
    public static class AppPaths
    {
        private static readonly string Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "jftwGo");

        public static string CacheRoot { get { return Ensure(Path.Combine(Root, "cache")); } }
        public static string IconCacheDir { get { return Ensure(Path.Combine(CacheRoot, "icons")); } }
        public static string CatalogCacheFile { get { return Path.Combine(CacheRoot, "resources.json"); } }
        public static string LogDir { get { return Ensure(Path.Combine(Root, "logs")); } }
        public static string TempDir { get { return Ensure(Path.Combine(Root, "temp")); } }
        public static string DownloadDir { get { return Ensure(Path.Combine(Root, "downloads")); } }

        public static string Ensure(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>生成带时间戳与随机后缀的临时文件路径。</summary>
        public static string NewTempFile(string extension)
        {
            if (string.IsNullOrEmpty(extension)) extension = ".tmp";
            if (!extension.StartsWith(".")) extension = "." + extension;
            return Path.Combine(TempDir,
                "jftw_" + Guid.NewGuid().ToString("N") + extension);
        }
    }
}
