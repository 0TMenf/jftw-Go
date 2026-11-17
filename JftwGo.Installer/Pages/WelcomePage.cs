using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

namespace JftwGo.Installer.Pages
{
    public class WelcomePage : WizardPage
    {
        public override string PageTitle { get { return "欢迎使用 jftw Go Installer"; } }

        public WelcomePage()
        {
            BackColor = UiTheme.Page;
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(24)
            };

            var title = new Label
            {
                Text = "jftw Go · U 盘启动盘制作器",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.AccentDark,
                Dock = DockStyle.Top,
                Height = 44
            };
            var intro = new Label
            {
                Text = "本工具用于在高配置电脑上，把选好的 jftw Go 资源写入 U 盘，制作成可携带的启动介质。\n\n" +
                       "使用前提：\n" +
                       "  • 一台运行 Windows 的电脑；\n" +
                       "  • 一个可正常读写的 U 盘（制作过程会清空其中数据）；\n" +
                       "  • 良好的网络连接用于下载所选资源。\n\n" +
                       "注意：本程序不是 jftw Go 的普通安装程序。若你只想在线安装并直接运行，\n" +
                       "请使用 jftw Go Direct。",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };
            var tip = new Label
            {
                Text = "请点击「下一步」开始。",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 28
            };
            card.Controls.Add(intro);
            card.Controls.Add(title);
            card.Controls.Add(tip);
            Controls.Add(card);
        }
    }
}
