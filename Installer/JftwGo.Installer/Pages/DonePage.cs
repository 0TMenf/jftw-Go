using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Installer.Pages
{
    public class DonePage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly Label _lblDetail;

        public override string PageTitle { get { return L.Get("done.title"); } }
        public override bool CanGoNext { get { return true; } }

        public DonePage(InstallContext ctx)
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

            var tip = new Label
            {
                Text = L.Get("done.tip"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            card.Controls.Add(_lblDetail);
            card.Controls.Add(title);
            card.Controls.Add(tip);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            var usb = _ctx.SelectedUsb;
            var res = _ctx.SelectedResource;
            var dep = _ctx.DeployResult;
            string extra = "";
            if (dep != null)
            {
                extra = string.Format(
                    L.Get("done.packageDir"),
                    res == null ? "-" : UsbWriter.GetPackageFolderName(res));
                if (dep.LauncherFound)
                    extra += (dep.ShortcutCreated && dep.BatCreated
                        ? L.Get("done.shortcutOk")
                        : L.Get("done.shortcutBad"));
                else
                    extra += (dep.Warning ?? "") + "\n";
            }
            _lblDetail.Text = string.Format(
                L.Get("done.body"),
                res == null ? L.Get("common.unknown") : res.ToString(),
                usb == null ? L.Get("common.unknown") : usb.DriveLetter,
                extra);
            if (Owner != null) Owner.UpdateNextButton();
        }

        public override bool OnNext()
        {
            var form = FindForm();
            if (form != null) form.Close();
            return true;
        }
    }
}
