using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JftwGo.Core.Api;
using JftwGo.Core.Icons;
using JftwGo.Core.Models;

namespace JftwGo.UI.Controls
{
    /// <summary>
    /// 共享的"资源浏览与选择"组件。
    /// Head 栏：官方整合包 / 历史版本 / 社区开发版本 / 单个游戏。
    /// 官方整合包下：稳定版 / 预览版 / 开发版 三个通道切换。
    /// 所有分类复用同一 ResourceCard 渲染。加载中/空/失败状态内置，可刷新。
    /// Installer 与 Direct 的"资源选择页"直接嵌入本控件。
    /// </summary>
    public class ResourceBrowser : UserControl
    {
        public enum HeadCategory { Official, Historical, Community, Games }
        public enum OfficialChannel { Stable, Preview, Dev }

        private readonly ApiClient _api;
        private readonly IconCache _icons;
        private ResourceCatalog _catalog;

        private HeadCategory _category = HeadCategory.Official;
        private OfficialChannel _channel = OfficialChannel.Stable;

        private readonly FlowLayoutPanel _headBar;
        private readonly FlowLayoutPanel _channelBar;
        private readonly Panel _listHost;
        private readonly FlowLayoutPanel _list;
        private readonly Label _status;
        private readonly Button _btnRefresh;
        private readonly Dictionary<string, Button> _headButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> _channelButtons = new Dictionary<string, Button>();
        private CancellationTokenSource _loadCts;

        public event EventHandler<ResourceItem> ResourceSelected;
        public ResourceItem SelectedResource { get; private set; }

        public ResourceBrowser(ApiClient api, IconCache icons)
        {
            _api = api;
            _icons = icons;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.Page;
            Padding = new Padding(14);

            var title = new Label
            {
                Text = "选择资源",
                Font = UiTheme.FontSubTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _headBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(4)
            };

            _channelBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(4)
            };

            _status = new Label
            {
                Text = "",
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 36 };
            _btnRefresh = UiTheme.MakeButton("刷新列表", false);
            _btnRefresh.Location = new Point(0, 2);
            _btnRefresh.Click += (s, e) => { _ = LoadAsync(); };
            toolbar.Controls.Add(_btnRefresh);

            _listHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Page };
            _list = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Dock = DockStyle.Top,
                Padding = new Padding(2),
                BackColor = UiTheme.Page
            };
            _listHost.Controls.Add(_list);

            Controls.Add(_listHost);
            Controls.Add(toolbar);
            Controls.Add(_status);
            Controls.Add(_channelBar);
            Controls.Add(_headBar);
            Controls.Add(title);

            BuildHeadBar();
            BuildChannelBar();
            Resize += (s, e) => LayoutList();
        }

        private void BuildHeadBar()
        {
            AddHeadButton("官方整合包", HeadCategory.Official);
            AddHeadButton("历史版本", HeadCategory.Historical);
            AddHeadButton("社区开发版本", HeadCategory.Community);
            AddHeadButton("单个游戏", HeadCategory.Games);
            UpdateHeadStyle();
        }

        private void AddHeadButton(string text, HeadCategory cat)
        {
            var b = new Button
            {
                Text = text,
                Font = UiTheme.FontButton,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.TextPrimary,
                Height = 32,
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(2),
                Cursor = Cursors.Hand,
                Tag = cat
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, e) =>
            {
                _category = (HeadCategory)b.Tag;
                SelectedResource = null;
                UpdateHeadStyle();
                RenderList();
            };
            _headButtons[cat.ToString()] = b;
            _headBar.Controls.Add(b);
        }

        private void BuildChannelBar()
        {
            AddChannelButton("稳定版", OfficialChannel.Stable);
            AddChannelButton("预览版", OfficialChannel.Preview);
            AddChannelButton("开发版", OfficialChannel.Dev);
            UpdateChannelStyle();
        }

        private void AddChannelButton(string text, OfficialChannel ch)
        {
            var b = new Button
            {
                Text = text,
                Font = UiTheme.FontSmall,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.TextSecondary,
                Height = 26,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(2),
                Cursor = Cursors.Hand,
                Tag = ch
            };
            b.FlatAppearance.BorderColor = UiTheme.CardBorder;
            b.Click += (s, e) =>
            {
                _channel = (OfficialChannel)b.Tag;
                SelectedResource = null;
                UpdateChannelStyle();
                RenderList();
            };
            _channelButtons[ch.ToString()] = b;
            _channelBar.Controls.Add(b);
        }

        private void UpdateHeadStyle()
        {
            foreach (var kv in _headButtons)
            {
                bool active = kv.Key == _category.ToString();
                var b = kv.Value;
                b.BackColor = active ? UiTheme.HeadActive : UiTheme.Surface;
                b.ForeColor = active ? UiTheme.AccentDark : UiTheme.TextPrimary;
                b.Font = new Font(UiTheme.FontButton.FontFamily, 9F,
                    active ? FontStyle.Bold : FontStyle.Regular);
            }
            _channelBar.Visible = _category == HeadCategory.Official;
        }

        private void UpdateChannelStyle()
        {
            foreach (var kv in _channelButtons)
            {
                bool active = kv.Key == _channel.ToString();
                var b = kv.Value;
                b.BackColor = active ? UiTheme.Accent : UiTheme.Surface;
                b.ForeColor = active ? Color.White : UiTheme.TextSecondary;
            }
        }

        /// <summary>异步加载资源（含状态渲染）。进入资源页时调用。</summary>
        public async Task LoadAsync()
        {
            if (_loadCts != null)
            {
                try { _loadCts.Cancel(); } catch { }
            }
            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;

            SetStatus("正在加载资源列表…", UiTheme.TextSecondary);
            ClearCards();
            AddMessageCard("正在加载资源列表，请稍候…");

            ResourceLoadResult result;
            try
            {
                result = await _api.LoadAsync(ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException) { return; }

            if (IsDisposed || !IsHandleCreated) return;

            if (result.State == ResourceLoadState.Failed)
            {
                _catalog = null;
                SetStatus("加载失败", UiTheme.Danger);
                ClearCards();
                AddMessageCard("资源列表加载失败：" + result.ErrorMessage + "\n可点击右上角「刷新列表」重试。", UiTheme.Danger);
                return;
            }

            _catalog = result.Catalog;
            string cacheTip = result.FromCache ? "（离线缓存）" : "";
            if (result.State == ResourceLoadState.Empty)
                SetStatus("资源列表为空" + cacheTip, UiTheme.TextSecondary);
            else
                SetStatus("共加载资源 " + CountAll() + " 项" + cacheTip, UiTheme.TextSecondary);
            RenderList();
        }

        private int CountAll()
        {
            if (_catalog == null || _catalog.Resources == null) return 0;
            var r = _catalog.Resources;
            int n = 0;
            if (r.Official != null)
            {
                if (IsOk(r.Official.Stable)) n++;
                if (IsOk(r.Official.Preview)) n++;
                if (IsOk(r.Official.Dev)) n++;
            }
            if (r.Historical != null) n += r.Historical.Count;
            if (r.Community != null) n += r.Community.Count;
            if (r.Games != null) n += r.Games.Count;
            return n;
        }

        private static bool IsOk(ResourceItem i) { return i != null && i.IsValid; }

        private void RenderList()
        {
            ClearCards();
            if (_catalog == null)
            {
                AddMessageCard("暂无资源，请点击「刷新列表」。", UiTheme.TextSecondary);
                return;
            }

            List<ResourceItem> items = CurrentItems();
            if (items.Count == 0)
            {
                AddMessageCard("该分类下暂无资源。", UiTheme.TextSecondary);
                return;
            }

            foreach (var item in items)
            {
                var card = new ResourceCard(_icons);
                card.Width = Math.Max(360, _listHost.ClientSize.Width - 24);
                card.Bind(item);
                card.Selected += (s, picked) =>
                {
                    SelectedResource = picked;
                    var h = ResourceSelected;
                    if (h != null) h(this, picked);
                };
                _list.Controls.Add(card);
            }
            LayoutList();
        }

        private List<ResourceItem> CurrentItems()
        {
            var result = new List<ResourceItem>();
            if (_catalog == null || _catalog.Resources == null) return result;
            var r = _catalog.Resources;
            switch (_category)
            {
                case HeadCategory.Official:
                    if (r.Official != null)
                    {
                        ResourceItem item = null;
                        if (_channel == OfficialChannel.Stable) item = r.Official.Stable;
                        else if (_channel == OfficialChannel.Preview) item = r.Official.Preview;
                        else item = r.Official.Dev;
                        if (IsOk(item)) result.Add(item);
                        else result.Add(new ResourceItem
                        {
                            Id = "offline-slot", Name = "该通道暂无资源", Description = "请切换通道或稍后刷新"
                        });
                    }
                    break;
                case HeadCategory.Historical:
                    if (r.Historical != null) result.AddRange(r.Historical);
                    break;
                case HeadCategory.Community:
                    if (r.Community != null) result.AddRange(r.Community);
                    break;
                case HeadCategory.Games:
                    if (r.Games != null) result.AddRange(r.Games);
                    break;
            }
            return result;
        }

        private void LayoutList()
        {
            int width = Math.Max(360, _listHost.ClientSize.Width - 24);
            foreach (Control c in _list.Controls)
                if (c is ResourceCard) c.Width = width;
        }

        private void ClearCards()
        {
            foreach (Control c in _list.Controls)
            {
                var card = c as ResourceCard;
                if (card != null) card.Dispose();
            }
            _list.Controls.Clear();
        }

        private void AddMessageCard(string text)
        {
            AddMessageCard(text, UiTheme.TextSecondary);
        }

        private void AddMessageCard(string text, Color color)
        {
            var lbl = new Label
            {
                Text = text,
                Font = UiTheme.FontNormal,
                ForeColor = color,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(360, _listHost.ClientSize.Width - 40), 0),
                Margin = new Padding(12, 20, 12, 12)
            };
            _list.Controls.Add(lbl);
        }

        private void SetStatus(string text, Color color)
        {
            if (_status.InvokeRequired)
            {
                _status.BeginInvoke(new Action(() => SetStatus(text, color)));
                return;
            }
            _status.Text = text;
            _status.ForeColor = color;
        }
    }
}
