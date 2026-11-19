using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LauncherGen
{
    internal static class Program
    {
        /// <summary>
        /// 正常启动显示 GUI。
        /// 命令行模式（便于无人值守/测试）：LauncherGen.exe --generate &lt;配置json&gt; &lt;输出目录&gt;
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "--generate")
            {
                GenerateHeadless(args[1], args[2]);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void GenerateHeadless(string configPath, string outDir)
        {
            try
            {
                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<LauncherConfig>(
                    File.ReadAllText(configPath, System.Text.Encoding.UTF8));
                if (cfg == null) throw new Exception("配置为空");
                Directory.CreateDirectory(outDir);

                string bgName = "";
                if (cfg.BackgroundMode == "image" && !string.IsNullOrEmpty(cfg.BackgroundImagePath)
                    && File.Exists(cfg.BackgroundImagePath))
                {
                    bgName = "launcher-bg" + Path.GetExtension(cfg.BackgroundImagePath).ToLowerInvariant();
                    File.Copy(cfg.BackgroundImagePath, Path.Combine(outDir, bgName), true);
                    cfg.BackgroundImagePath = Path.Combine(outDir, bgName);
                }

                // 相对化游戏路径
                foreach (var g in cfg.Games)
                {
                    if (!string.IsNullOrEmpty(g.RelativePath) && Path.IsPathRooted(g.RelativePath))
                        g.RelativePath = MainForm.ToRelativePublic(outDir, g.RelativePath);
                    if (!string.IsNullOrEmpty(g.WorkDir) && Path.IsPathRooted(g.WorkDir))
                        g.WorkDir = MainForm.ToRelativePublic(outDir, g.WorkDir);
                }

                string csrc = CodeEmitter.Emit(cfg, bgName);
                string cPath = Path.Combine(outDir, "start1.c");
                File.WriteAllText(cPath, csrc, new UTF8Encoding(false));

                string zig = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "ziglang", "ziglang", "zig.exe"));
                if (!File.Exists(zig)) throw new FileNotFoundException("zig.exe not found: " + zig);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = zig,
                    Arguments = string.Format("cc -target x86_64-windows-gnu -O2 -municode \"{0}\" -o \"{1}\" -lgdiplus -lshell32 -luser32 -lgdi32 -lm",
                        cPath, Path.Combine(outDir, "start1.exe")),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = outDir
                };
                var proc = System.Diagnostics.Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode != 0) throw new Exception("zig compile failed: " + err);

                Console.WriteLine("OK: " + Path.Combine(outDir, "start1.exe"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
