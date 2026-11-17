using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

namespace JftwGo.Direct.Pages
{
    public class WelcomePage : WizardPage
    {
        public override string PageTitle { get { return "欢迎使用 jftw Go Direct"; } }

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
                Text = "jftw Go · 在线直装（无需 U 盘）",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.AccentDark,
                Dock = DockStyle.Top,
                Height = 44
            };
            var intro = new Label
            {
                Text = "本工具适用于没有 U 盘、但网络条件较好的电脑：\n" +
                       "在线下载 jftw Go 资源，解压安装到你指定的目录，并可直接启动。\n\n" +
                       "使用前提：\n" +
                       "  • 可正常访问资源站点的网络；\n" +
                       "  • 一个可写的安装目录（建议非系统盘）。\n\n" +
                       "注意：本程序需要联网下载资源，不做 U 盘制作。\n" +
                       "若你要制作可携带的 U 盘启动盘，请使用 jftw Go Installer。",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill
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
