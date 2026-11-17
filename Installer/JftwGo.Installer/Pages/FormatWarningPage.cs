using System;
using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Installer.Pages
{
    /// <summary>
    /// 部署确认页：说明将创建的目录结构，检测 jftw_Go_Assets 是否被占用。
    /// 占用时不阻断（允许向 version 下追加整合包），但必须勾选"我已了解"才能继续。
    /// 本页不再包含任何格式化/清盘操作。
    /// </summary>
    public class FormatWarningPage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly Label _lblSummary;
        private readonly Label _lblOccupied;
        private readonly CheckBox _chkConfirm;

        public override string PageTitle { get { return L.Get("deploy.title"); } }

        public override bool CanGoNext { get { return _chkConfirm.Checked; } }

        public FormatWarningPage(InstallContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(22) };

            var title = new Label
            {
                Text = L.Get("deploy.heading"),
                Font = new Font(UiTheme.FontTitle.FontFamily, 13F, FontStyle.Bold),
                ForeColor = UiTheme.AccentDark,
                Dock = DockStyle.Top,
                Height = 34
            };

            _lblSummary = new Label
            {
                Text = "",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 128
            };

            var structure = new Label
            {
                Text = L.Get("deploy.structure"),
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 116
            };

            _lblOccupied = new Label
            {
                Text = "",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.Danger,
                Dock = DockStyle.Top,
                Height = 32
            };

            _chkConfirm = new CheckBox
            {
                Text = L.Get("deploy.check"),
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };
            _chkConfirm.CheckedChanged += (s, e) => { if (Owner != null) Owner.UpdateNextButton(); };

            card.Controls.Add(_chkConfirm);
            card.Controls.Add(_lblOccupied);
            card.Controls.Add(structure);
            card.Controls.Add(_lblSummary);
            card.Controls.Add(title);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            _chkConfirm.Checked = false;
            var res = _ctx.SelectedResource;
            var usb = _ctx.SelectedUsb;
            _lblSummary.Text = string.Format(
                L.Get("deploy.summary"),
                res == null ? L.Get("common.none") : res.ToString(),
                usb == null ? L.Get("common.none") : usb.DriveLetter,
                usb == null ? "-" : usb.FileSystem,
                usb == null ? "-" : usb.FreeText,
                res == null ? "-" : UsbWriter.GetPackageFolderName(res));

            bool occupied = usb != null && UsbWriter.IsAssetsFolderOccupied(usb);
            _lblOccupied.Text = occupied
                ? L.Get("deploy.occupied")
                : "";
            _lblOccupied.ForeColor = occupied ? UiTheme.Danger : UiTheme.TextSecondary;
            if (Owner != null) Owner.UpdateNextButton();
        }
    }
}
