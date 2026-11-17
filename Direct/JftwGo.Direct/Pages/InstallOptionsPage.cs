using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

namespace JftwGo.Direct.Pages
{
    /// <summary>安装路径与启动选项页。</summary>
    public class InstallOptionsPage : WizardPage
    {
        private readonly DirectContext _ctx;
        private readonly TextBox _txtPath;
        private readonly CheckBox _chkLaunch;

        public override string PageTitle { get { return "选择安装位置"; } }

        public override bool CanGoNext
        {
            get
            {
                string p = _txtPath.Text.Trim();
                if (p.Length == 0) return false;
                try { return Path.IsPathRooted(p); }
                catch { return false; }
            }
        }

        public InstallOptionsPage(DirectContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(22) };

            var title = new Label
            {
                Text = "安装位置",
                Font = UiTheme.FontSubTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };

            var row = new Panel { Dock = DockStyle.Top, Height = 44 };
            _txtPath = new TextBox
            {
                Font = UiTheme.FontNormal,
                Width = 460,
                Location = new Point(0, 8)
            };
            var btnBrowse = UiTheme.MakeButton("浏览…", false);
            btnBrowse.Location = new Point(470, 6);
            btnBrowse.Click += (s, e) => Browse();
            row.Controls.Add(_txtPath);
            row.Controls.Add(btnBrowse);

            var hint = new Label
            {
                Text = "建议安装到非系统盘或用户目录下的独立文件夹。安装会覆盖目标目录中的同名文件。",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 40
            };

            _chkLaunch = new CheckBox
            {
                Text = "安装完成后立即启动 jftw Go",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30,
                Checked = true
            };
            _chkLaunch.CheckedChanged += (s, e) => _ctx.LaunchAfterInstall = _chkLaunch.Checked;

            card.Controls.Add(_chkLaunch);
            card.Controls.Add(hint);
            card.Controls.Add(row);
            card.Controls.Add(title);
            Controls.Add(card);

            string def = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jftwGo", "app");
            _txtPath.Text = def;
            _txtPath.TextChanged += (s, e) => { if (Owner != null) Owner.UpdateNextButton(); };
        }

        private void Browse()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择 jftw Go 的安装目录";
                dlg.ShowNewFolderButton = true;
                try
                {
                    string cur = _txtPath.Text.Trim();
                    if (Directory.Exists(cur)) dlg.SelectedPath = cur;
                }
                catch { }
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    _txtPath.Text = dlg.SelectedPath;
            }
        }

        public override void OnEnter()
        {
            _ctx.InstallDir = _txtPath.Text.Trim();
            _ctx.LaunchAfterInstall = _chkLaunch.Checked;
            if (Owner != null) Owner.UpdateNextButton();
        }

        public override bool OnNext()
        {
            _ctx.InstallDir = _txtPath.Text.Trim();
            _ctx.LaunchAfterInstall = _chkLaunch.Checked;
            return false;
        }
    }
}
