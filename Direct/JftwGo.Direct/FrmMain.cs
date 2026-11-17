using System;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Logging;
using JftwGo.Core.Models;
using JftwGo.Direct.Pages;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Direct
{
    /// <summary>Direct 向导共享上下文。</summary>
    public class DirectContext
    {
        public ILogger Logger;
        public ApiClient Api;
        public IconCache Icons;
        public ResourceItem SelectedResource;
        public string InstallDir;
        public bool LaunchAfterInstall = true;
        public string LaunchExePath;
    }

    public class FrmMain : WizardForm
    {
        public DirectContext Context { get; private set; }

        private readonly WelcomePage _welcome;
        private readonly ResourcePage _resource;
        private readonly InstallOptionsPage _options;
        private readonly DownloadInstallPage _install;
        private readonly DonePage _done;

        public FrmMain(ILogger logger, ApiClient api, IconCache icons)
        {
            Context = new DirectContext { Logger = logger, Api = api, Icons = icons };
            Text = L.Get("app.title");

            _welcome = new WelcomePage();
            _resource = new ResourcePage(Context);
            _options = new InstallOptionsPage(Context);
            _install = new DownloadInstallPage(Context);
            _done = new DonePage(Context);

            AddPage(_welcome);
            AddPage(_resource);
            AddPage(_options);
            AddPage(_install);
            AddPage(_done);

            _install.OnFinished = () =>
            {
                SetNextText(L.Get("common.finish"));
                SetNextEnabled(true);
            };
            Start();
        }
    }
}
