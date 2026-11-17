using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

namespace JftwGo.Installer.Pages
{
    public class DonePage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly Label _lblDetail;

        public override string PageTitle { get { return "制作完成"; } }
        public override bool CanGoNext { get { return true; } }

        public DonePage(InstallContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(24) };

            var title = new Label
            {
                Text = "U 盘启动盘制作完成",
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
                Text = "你可以安全拔出 U 盘，或返回重新制作另一块。",
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
            _lblDetail.Text = string.Format(
                "资源：{0}\n目标 U 盘：{1}\n\n" +
                "U 盘根目录已写入所选资源，并生成 jftwgo-media.txt 制作标记。\n" +
                "如果资源包内含引导文件，将随包一并释放到盘根。",
                res == null ? "未知" : res.ToString(),
                usb == null ? "未知" : usb.DriveLetter);
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
