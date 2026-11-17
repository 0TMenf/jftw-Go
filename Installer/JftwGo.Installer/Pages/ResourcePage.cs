using System.Windows.Forms;
using JftwGo.Core.Models;
using JftwGo.UI.Controls;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Installer.Pages
{
    /// <summary>资源选择页：嵌入共享 ResourceBrowser，选中后写入上下文。</summary>
    public class ResourcePage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly ResourceBrowser _browser;

        public override string PageTitle { get { return L.Get("res.title"); } }

        public override bool CanGoNext
        {
            get { return _ctx.SelectedResource != null && _ctx.SelectedResource.IsValid; }
        }

        public ResourcePage(InstallContext ctx)
        {
            _ctx = ctx;
            _browser = new ResourceBrowser(ctx.Api, ctx.Icons) { Dock = DockStyle.Fill };
            _browser.ResourceSelected += (s, item) =>
            {
                _ctx.SelectedResource = item;
                if (Owner != null) Owner.UpdateNextButton();
            };
            Controls.Add(_browser);
        }

        public override void OnEnter()
        {
            if (!_entered)
            {
                _entered = true;
                var ignore = _browser.LoadAsync();
            }
            if (Owner != null) Owner.UpdateNextButton();
        }

        private bool _entered;
    }
}
