using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Direct.Pages
{
    public class DonePage : WizardPage
    {
        private readonly DirectContext _ctx;
        private readonly Label _lblDetail;
        private readonly Button _btnOpen;

        public override string PageTitle { get { return L.Get("done.title"); } }
        public override bool CanGoNext { get { return true; } }

        public DonePage(DirectContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(24) };

            var title = new Label
            {
                Text = L.Get("done.heading"),
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.Success,
                Dock = DockStyle.Top,
                Height = 44
            };

            _lblDetail = new Label
            {
                Text = "",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill
            };

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            _btnOpen = UiTheme.MakeButton(L.Get("done.openDir"), false);
            _btnOpen.Location = new Point(0, 6);
            _btnOpen.Click += (s, e) => OpenFolder();
            bar.Controls.Add(_btnOpen);

            card.Controls.Add(_lblDetail);
            card.Controls.Add(title);
            card.Controls.Add(bar);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            var res = _ctx.SelectedResource;
            _lblDetail.Text = string.Format(
                L.Get("done.body") + "{2}",
                res == null ? L.Get("common.unknown") : res.ToString(),
                _ctx.InstallDir,
                _ctx.LaunchAfterInstall ? L.Get("done.launched") : L.Get("done.notLaunched"));
            if (Owner != null) Owner.UpdateNextButton();
        }

        private void OpenFolder()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_ctx.InstallDir) && Directory.Exists(_ctx.InstallDir))
                    Process.Start("explorer.exe", "\"" + _ctx.InstallDir + "\"");
            }
            catch { }
        }

        public override bool OnNext()
        {
            var form = FindForm();
            if (form != null) form.Close();
            return true;
        }
    }
}
