using System;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Logging;

namespace JftwGo.Direct
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var logger = new FileLogger();
            var api = new ApiClient(ApiOptions.Default, logger);
            var icons = new IconCache(logger);

            Application.Run(new FrmMain(logger, api, icons));
        }
    }
}
