using System;
using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

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

        public override string PageTitle { get { return "确认部署"; } }

        public override bool CanGoNext { get { return _chkConfirm.Checked; } }

        public FormatWarningPage(InstallContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(22) };

            var title = new Label
            {
                Text = "确认资源部署信息",
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
                Text = "将在 U 盘上创建如下结构（不会格式化或删除现有文件）：\n" +
                       "  jftw_Go_Assets\\version\\<整合包>\\   ← 存放所选整合包\n" +
                       "  jftw Go.lnk  /  jftw Go.bat            ← 根目录启动入口\n\n" +
                       "多个整合包可以共存：每个整合包在 version 下拥有独立文件夹，\n" +
                       "类似 Minecraft 的 .minecraft\\version 结构。",
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
                Text = "我已了解上述部署方式，确认继续。",
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
                "将部署资源：{0}\n目标 U 盘：{1}（{2}，可用 {3}）\n整合包文件夹：jftw_Go_Assets\\version\\{4}",
                res == null ? "未选择" : res.ToString(),
                usb == null ? "未选择" : usb.DriveLetter,
                usb == null ? "-" : usb.FileSystem,
                usb == null ? "-" : usb.FreeText,
                res == null ? "-" : UsbWriter.GetPackageFolderName(res));

            bool occupied = usb != null && UsbWriter.IsAssetsFolderOccupied(usb);
            _lblOccupied.Text = occupied
                ? "注意：U 盘上已存在 jftw_Go_Assets 文件夹。将向其 version 目录追加/更新该整合包，现有其它文件不会被删除。"
                : "";
            _lblOccupied.ForeColor = occupied ? UiTheme.Danger : UiTheme.TextSecondary;
            if (Owner != null) Owner.UpdateNextButton();
        }
    }
}
