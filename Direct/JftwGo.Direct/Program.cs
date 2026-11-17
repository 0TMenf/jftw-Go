using System;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Logging;
using JftwGo.Core.Localization;

namespace JftwGo.Direct
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            L.Initialize(); // 加载语言包（zh_cn / zh_tw / zh_hk）

            var logger = new FileLogger();
            var api = new ApiClient(ApiOptions.Default, logger);
            var icons = new IconCache(logger);

            Application.Run(new FrmMain(logger, api, icons));
        }
    }
}
