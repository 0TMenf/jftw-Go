using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JftwGo.UI.Wizard
{
    /// <summary>
    /// 向导壳：顶部标题 + 中部页面容器 + 底部导航按钮（上一步/下一步/取消）。
    /// 子类在构造时通过 AddPage 添加页面；提供步骤指示、确认与错误对话框辅助。
    /// </summary>
    public class WizardForm : Form
    {
        private readonly List<WizardPage> _pages = new List<WizardPage>();
        private readonly Label _lblTitle;
        private readonly Label _lblStep;
        private readonly Panel _pageHost;
        private readonly Button _btnBack;
        private readonly Button _btnNext;
        private readonly Button _btnCancel;
        private int _index = -1;
        private bool _navigating;

        public WizardForm()
        {
            UiTheme.PrepareForm(this);
            Width = 860;
            Height = 620;
            MinimumSize = new Size(760, 540);
            Text = "jftw Go";

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UiTheme.Surface };
            _lblTitle = new Label
            {
                Text = "jftw Go",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(18, 8, 0, 0)
            };
            _lblStep = new Label
            {
                Text = "",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 22,
                Padding = new Padding(20, 0, 0, 0)
            };
            header.Controls.Add(_lblStep);
            header.Controls.Add(_lblTitle);
            var line = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiTheme.CardBorder };
            header.Controls.Add(line);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = UiTheme.Surface };
            _btnCancel = UiTheme.MakeButton("取消", false);
            _btnBack = UiTheme.MakeButton("上一步", false);
            _btnNext = UiTheme.MakeButton("下一步", true);

            _btnCancel.Click += (s, e) => OnCancelClicked();
            _btnBack.Click += (s, e) => GoBack();
            _btnNext.Click += (s, e) => GoNext();

            // 用布局容器自动排布，避免依赖窗体宽度做硬编码坐标（容器宽度在构造时尚未确定）。
            var leftNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(18, 11, 0, 0)
            };
            leftNav.Controls.Add(_btnCancel);

            var rightNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 11, 18, 0)
            };
            rightNav.Controls.Add(_btnNext);   // RightToLeft：先添加的排在最右
            rightNav.Controls.Add(_btnBack);   // 其后排在其左侧

            footer.Controls.Add(rightNav);
            footer.Controls.Add(leftNav);
            var topLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UiTheme.CardBorder };
            footer.Controls.Add(topLine);

            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page, Padding = new Padding(14) };

            Controls.Add(_pageHost);
            Controls.Add(footer);
            Controls.Add(header);
        }

        protected void AddPage(WizardPage page)
        {
            page.Owner = this;
            page.Dock = DockStyle.Fill;
            _pages.Add(page);
        }

        protected void Start()
        {
            if (_pages.Count == 0) return;
            ShowPage(0);
        }

        public int CurrentIndex { get { return _index; } }
        public int PageCount { get { return _pages.Count; } }

        public void GoNext()
        {
            if (_navigating || _index < 0 || _index >= _pages.Count) return;
            var current = _pages[_index];
            if (!current.CanGoNext)
            {
                MessageBox.Show(this, "请先完成当前步骤的必填项。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (current.OnNext()) return; // 页面拦截了导航
            if (_index < _pages.Count - 1)
                ShowPage(_index + 1);
        }

        public void GoBack()
        {
            if (_navigating || _index <= 0) return;
            var current = _pages[_index];
            if (current.OnBack()) return;
            ShowPage(_index - 1);
        }

        /// <summary>跳转到指定页（用于完成后返回）。</summary>
        public void GoTo(int index)
        {
            if (index >= 0 && index < _pages.Count) ShowPage(index);
        }

        private void ShowPage(int index)
        {
            _navigating = true;
            try
            {
                if (_index >= 0 && _index < _pages.Count)
                {
                    _pages[_index].OnLeave();
                    _pages[_index].Visible = false;
                }
                _index = index;
                var page = _pages[index];
                if (!_pageHost.Controls.Contains(page)) _pageHost.Controls.Add(page);
                page.Visible = true;
                page.BringToFront();
                _lblTitle.Text = page.PageTitle;
                _lblStep.Text = "步骤 " + (index + 1) + " / " + _pages.Count;

                _btnBack.Enabled = index > 0;
                bool last = index == _pages.Count - 1;
                _btnNext.Text = last ? "完成" : "下一步";
                _btnNext.Enabled = page.CanGoNext;
                page.OnEnter();
                UpdateNextButton();
            }
            finally
            {
                _navigating = false;
            }
        }

        /// <summary>页面内容变化后调用，刷新"下一步"可用状态。</summary>
        public void UpdateNextButton()
        {
            if (_index < 0 || _index >= _pages.Count) return;
            _btnNext.Enabled = _pages[_index].CanGoNext;
        }

        protected virtual void OnCancelClicked()
        {
            if (MessageBox.Show(this, "确定要退出向导吗？", "确认退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        }

        protected void SetNextText(string text) { _btnNext.Text = text; }
        public void SetNextEnabled(bool enabled) { _btnNext.Enabled = enabled; }
        protected void SetCancelText(string text) { _btnCancel.Text = text; }
        public void SetNextVisible(bool visible) { _btnNext.Visible = visible; }
        public void SetBackVisible(bool visible) { _btnBack.Visible = visible; }
        public void SetCancelVisible(bool visible) { _btnCancel.Visible = visible; }

        public static DialogResult Confirm(Form owner, string message, string caption, bool danger)
        {
            return MessageBox.Show(owner, message, caption, MessageBoxButtons.OKCancel,
                danger ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
        }

        public static void ShowError(Form owner, string message)
        {
            MessageBox.Show(owner, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowInfo(Form owner, string message)
        {
            MessageBox.Show(owner, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
