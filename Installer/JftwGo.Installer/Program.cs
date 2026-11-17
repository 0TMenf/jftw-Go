using System;
using System.IO;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Logging;
using JftwGo.Core.Localization;

namespace JftwGo.Installer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 全局异常兜底：WinExe 没有控制台，未捕获异常会静默退出（表现为"双击没反应"）。
            Application.ThreadException += (s, e) => ShowFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                ShowFatal(e.ExceptionObject as Exception ?? new Exception("未知致命错误"));

            try
            {
                WriteStartup("启动中…");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                L.Initialize(); // 加载语言包（zh_cn / zh_tw / zh_hk）
                WriteStartup("语言已加载：" + L.Language);

                var logger = new FileLogger();
                var api = new ApiClient(ApiOptions.Default, logger);
                var icons = new IconCache(logger);
                WriteStartup("开始创建主窗口…");

                Application.Run(new FrmMain(logger, api, icons));
                WriteStartup("正常退出");
            }
            catch (Exception ex)
            {
                ShowFatal(ex);
            }
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                string msg = "[jftw Go Installer 错误]\n" + ex;
                WriteStartup("异常：" + ex);
                MessageBox.Show(msg, "jftw Go Installer 启动失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }

        private static void WriteStartup(string line)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jftwGo");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "startup-installer.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine);
            }
            catch { }
        }
    }
}
