using System;
using System.Drawing;
using System.Windows.Forms;
using JftwGo.UI;
using JftwGo.UI.Wizard;

namespace JftwGo.Installer.Pages
{
    /// <summary>
    /// 格式化/写入警告页：红色警示 + 二次确认。
    /// 二次确认 = 勾选「已了解」并手动输入盘符，避免误操作。
    /// </summary>
    public class FormatWarningPage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly Label _lblSummary;
        private readonly CheckBox _chkConfirm;
        private readonly TextBox _txtConfirm;
        private readonly Label _lblPrompt;

        public override string PageTitle { get { return "写入前确认"; } }

        public override bool CanGoNext
        {
            get
            {
                if (_ctx.SelectedUsb == null) return false;
                if (!_chkConfirm.Checked) return false;
                string expected = Normalize(_ctx.SelectedUsb.DriveLetter);
                return string.Equals(Normalize(_txtConfirm.Text), expected, StringComparison.OrdinalIgnoreCase)
                       && expected.Length > 0;
            }
        }

        public FormatWarningPage(InstallContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(22) };

            var title = new Label
            {
                Text = "警告：即将向 U 盘写入数据",
                Font = new Font(UiTheme.FontTitle.FontFamily, 13F, FontStyle.Bold),
                ForeColor = UiTheme.Danger,
                Dock = DockStyle.Top,
                Height = 34
            };

            _lblSummary = new Label
            {
                Text = "",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 108
            };

            var danger = new Label
            {
                Text = "以下操作不可撤销：\n" +
                       "  • 所选 U 盘上的原有数据将全部被清除；\n" +
                       "  • 写入过程中请勿拔出 U 盘或断电。\n\n" +
                       "请确认已备份重要文件后再继续。",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.Danger,
                Dock = DockStyle.Top,
                Height = 112
            };

            _chkConfirm = new CheckBox
            {
                Text = "我已备份该 U 盘上的重要数据，并了解写入会清空该盘。",
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 28
            };
            _chkConfirm.CheckedChanged += (s, e) => { if (Owner != null) Owner.UpdateNextButton(); };

            var confirmRow = new Panel { Dock = DockStyle.Top, Height = 62 };
            _lblPrompt = new Label
            {
                Text = "请输入盘符以确认（例如 E:）：",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(0, 0),
                AutoSize = true
            };
            _txtConfirm = new TextBox
            {
                Font = UiTheme.FontNormal,
                Location = new Point(0, 24),
                Width = 120
            };
            _txtConfirm.TextChanged += (s, e) => { if (Owner != null) Owner.UpdateNextButton(); };
            confirmRow.Controls.Add(_lblPrompt);
            confirmRow.Controls.Add(_txtConfirm);

            card.Controls.Add(confirmRow);
            card.Controls.Add(_chkConfirm);
            card.Controls.Add(danger);
            card.Controls.Add(_lblSummary);
            card.Controls.Add(title);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            _chkConfirm.Checked = false;
            _txtConfirm.Clear();
            var res = _ctx.SelectedResource;
            var usb = _ctx.SelectedUsb;
            _lblSummary.Text = string.Format(
                "将写入资源：{0}\n目标 U 盘：{1}（{2}，{3}）\n\n{4}",
                res == null ? "未选择" : res.ToString(),
                usb == null ? "未选择" : usb.DriveLetter,
                usb == null ? "-" : usb.FileSystem,
                usb == null ? "-" : usb.CapacityText,
                res != null && !string.IsNullOrWhiteSpace(res.Description) ? res.Description : "");
            if (Owner != null) Owner.UpdateNextButton();
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            string t = s.Trim().ToUpperInvariant().TrimEnd('\\');
            if (t.Length == 1 && t[0] >= 'A' && t[0] <= 'Z') t += ":";
            return t;
        }
    }
}
