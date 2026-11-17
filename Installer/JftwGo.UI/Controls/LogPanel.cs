using System;
using System.Drawing;
using System.Windows.Forms;
using JftwGo.Core.Logging;

namespace JftwGo.UI.Controls
{
    /// <summary>可展开的日志区：一个折叠按钮 + 多行只读文本框，订阅 ILogger 事件。</summary>
    public class LogPanel : UserControl
    {
        private readonly TextBox _text;
        private readonly Button _toggle;
        private bool _expanded;

        public LogPanel()
        {
            BackColor = UiTheme.Page;
            _toggle = new Button
            {
                Text = "展开日志 ▾",
                Font = UiTheme.FontSmall,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.AccentDark,
                Height = 26,
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand
            };
            _toggle.FlatAppearance.BorderColor = UiTheme.CardBorder;
            _toggle.Click += (s, e) => Toggle();

            _text = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiTheme.FontMono,
                BackColor = Color.FromArgb(0xFB, 0xFB, 0xFC),
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill,
                WordWrap = false
            };

            Controls.Add(_text);
            Controls.Add(_toggle);
            Height = 28;
        }

        private void Toggle()
        {
            _expanded = !_expanded;
            Height = _expanded ? 180 : 28;
            _toggle.Text = _expanded ? "收起日志 ▴" : "展开日志 ▾";
        }

        public void Attach(ILogger logger)
        {
            if (logger == null) return;
            logger.Logged += (s, e) => Append(e.Formatted);
        }

        public void Append(string line)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (_text.InvokeRequired)
            {
                _text.BeginInvoke(new Action(() => Append(line)));
                return;
            }
            _text.AppendText(line + Environment.NewLine);
        }

        public void Clear()
        {
            if (_text.InvokeRequired) { _text.BeginInvoke(new Action(Clear)); return; }
            _text.Clear();
        }
    }
}
