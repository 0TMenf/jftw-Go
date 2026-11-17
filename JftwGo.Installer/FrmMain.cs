using System;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Logging;
using JftwGo.Core.Models;
using JftwGo.Core.Usb;
using JftwGo.Installer.Pages;
using JftwGo.UI.Wizard;

namespace JftwGo.Installer
{
    /// <summary>向导内各页面共享的上下文。</summary>
    public class InstallContext
    {
        public ILogger Logger;
        public ApiClient Api;
        public IconCache Icons;
        public ResourceItem SelectedResource;
        public UsbDeviceInfo SelectedUsb;
        public string TargetPath;
    }

    public class FrmMain : WizardForm
    {
        public InstallContext Context { get; private set; }

        private readonly WelcomePage _welcome;
        private readonly ResourcePage _resource;
        private readonly UsbSelectPage _usb;
        private readonly FormatWarningPage _warn;
        private readonly WriteProgressPage _write;
        private readonly DonePage _done;

        public FrmMain(ILogger logger, ApiClient api, IconCache icons)
        {
            Context = new InstallContext { Logger = logger, Api = api, Icons = icons };
            Text = "jftw Go Installer — U 盘启动盘制作器";

            _welcome = new WelcomePage();
            _resource = new ResourcePage(Context);
            _usb = new UsbSelectPage(Context);
            _warn = new FormatWarningPage(Context);
            _write = new WriteProgressPage(Context);
            _done = new DonePage(Context);

            AddPage(_welcome);
            AddPage(_resource);
            AddPage(_usb);
            AddPage(_warn);
            AddPage(_write);
            AddPage(_done);

            _write.OnFinished = () =>
            {
                SetNextText("完成");
                SetNextEnabled(true);
            };
            Start();
        }
    }
}
