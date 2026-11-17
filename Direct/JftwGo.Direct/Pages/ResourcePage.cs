using System.Windows.Forms;
using JftwGo.UI.Controls;
using JftwGo.UI.Wizard;

namespace JftwGo.Direct.Pages
{
    /// <summary>资源选择页（与 Installer 共用 ResourceBrowser 组件）。</summary>
    public class ResourcePage : WizardPage
    {
        private readonly DirectContext _ctx;
        private readonly ResourceBrowser _browser;
        private bool _entered;

        public override string PageTitle { get { return "选择 jftw Go 资源"; } }

        public override bool CanGoNext
        {
            get { return _ctx.SelectedResource != null && _ctx.SelectedResource.IsValid; }
        }

        public ResourcePage(DirectContext ctx)
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
    }
}
