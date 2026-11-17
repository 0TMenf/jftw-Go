using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

using JftwGo.Core.Localization;

namespace JftwGo.Core.Packaging
{
    /// <summary>
    /// 压缩包解压（zip）。Direct 安装时使用；同时兼容把资源直接复制到目标目录（若非 zip）。
    /// </summary>
    public static class ArchiveExtractor
    {
        public static bool IsZip(string file)
        {
            try
            {
                using (var fs = File.OpenRead(file))
                {
                    var head = new byte[4];
                    if (fs.Read(head, 0, 4) < 4) return false;
                    return head[0] == 0x50 && head[1] == 0x4B && (head[2] == 0x03 || head[2] == 0x05 || head[2] == 0x07);
                }
            }
            catch { return false; }
        }

        /// <summary>异步解压 zip 到目标目录，支持取消与进度（按文件数粗粒度）。</summary>
        public static Task ExtractZipAsync(string zipPath, string targetDir,
            IProgress<int> progress, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    int total = archive.Entries.Count;
                    int done = 0;
                    foreach (var entry in archive.Entries)
                    {
                        ct.ThrowIfCancellationRequested();
                        string full = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                        string rootFull = Path.GetFullPath(targetDir);
                        // 防止 zip slip 路径穿越
                        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException(L.Format("package.illegalPath", entry.FullName));

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(full);
                        }
                        else
                        {
                            string parent = Path.GetDirectoryName(full);
                            if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                            entry.ExtractToFile(full, true);
                        }
                        done++;
                        if (progress != null && total > 0)
                            progress.Report((int)(done * 100L / total));
                    }
                }
            }, ct);
        }

        /// <summary>非 zip 文件：直接复制为目标目录下的同名文件。</summary>
        public static void CopyFileToDir(string sourceFile, string targetDir)
        {
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            string dest = Path.Combine(targetDir, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, dest, true);
        }
    }
}
