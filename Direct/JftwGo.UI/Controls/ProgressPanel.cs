using System;
using System.Drawing;
using System.Windows.Forms;

using JftwGo.Core.Localization;

namespace JftwGo.UI.Controls
{
    /// <summary>进度区：进度条 + 百分比 + 状态文本 + 速度，供下载/写入/解压复用。</summary>
    public class ProgressPanel : UserControl
    {
        private readonly ProgressBar _bar;
        private readonly Label _lblPercent;
        private readonly Label _lblStatus;
        private readonly Label _lblSpeed;

        public ProgressPanel()
        {
            BackColor = UiTheme.Page;
            Height = 72;
            _bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = UiTheme.Accent
            };
            _lblPercent = new Label
            {
                Text = "0%",
                Font = UiTheme.FontSubTitle,
                ForeColor = UiTheme.AccentDark,
                Dock = DockStyle.Right,
                Width = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblStatus = new Label
            {
                Text = L.Get("progress.ready"),
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 22, 0, 0)
            };
            _lblSpeed = new Label
            {
                Text = "",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 18,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_lblStatus);
            Controls.Add(_lblPercent);
            Controls.Add(_lblSpeed);
            Controls.Add(_bar);
        }

        public void SetProgress(int percent, string status, string speed)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetProgress(percent, status, speed)));
                return;
            }
            _bar.Value = Math.Max(0, Math.Min(100, percent));
            _lblPercent.Text = percent + "%";
            if (status != null) _lblStatus.Text = status;
            if (speed != null) _lblSpeed.Text = speed;
        }

        public void SetStatus(string status)
        {
            SetProgress(_bar.Value, status, null);
        }

        public void SetIndeterminate(bool indeterminate)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetIndeterminate(indeterminate))); return; }
            _bar.Style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            _lblPercent.Visible = !indeterminate;
        }
    }
}
