using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JftwGo.Core.Icons;
using JftwGo.Core.Models;
using JftwGo.Core.Usb;

using JftwGo.Core.Localization;

namespace JftwGo.UI.Controls
{
    /// <summary>
    /// 单个资源卡片：图标(异步/缩略图) + 名称/版本 + 描述 + 大小/更新时间 + 标签 + 选择按钮。
    /// 同一控件被官方三通道、历史、社区、游戏全部复用。
    /// </summary>
    public class ResourceCard : UserControl
    {
        private readonly IconCache _icons;
        private CancellationTokenSource _iconCts;
        private ResourceItem _item;
        private int _token;

        private readonly PictureBox _icon;
        private readonly Label _lblName;
        private readonly Label _lblDesc;
        private readonly Label _lblMeta;
        private readonly FlowLayoutPanel _tags;
        private readonly Button _btnSelect;

        public event EventHandler<ResourceItem> Selected;

        public ResourceItem Item { get { return _item; } }

        public ResourceCard(IconCache icons)
        {
            _icons = icons;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = UiTheme.Surface;
            Padding = new Padding(10);
            MinimumSize = new Size(360, 104);

            _icon = new PictureBox
            {
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(10, 10),
                Image = IconCache.Placeholder
            };

            _lblName = new Label
            {
                Font = new Font(UiTheme.FontNormal.FontFamily, 10F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                Location = new Point(68, 10),
                AutoSize = true,
                MaximumSize = new Size(260, 0)
            };

            _lblDesc = new Label
            {
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(68, 32),
                AutoSize = true,
                MaximumSize = new Size(300, 28)
            };

            _lblMeta = new Label
            {
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(68, 56),
                AutoSize = true,
                MaximumSize = new Size(300, 20)
            };

            _tags = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(68, 76),
                Size = new Size(300, 24),
                Margin = Padding.Empty
            };

            _btnSelect = UiTheme.MakeButton(L.Get("res.select"), true);
            _btnSelect.Tag = "lang:res.select";
            _btnSelect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSelect.Click += (s, e) =>
            {
                var h = Selected;
                if (h != null && _item != null) h(this, _item);
            };

            Controls.Add(_icon);
            Controls.Add(_lblName);
            Controls.Add(_lblDesc);
            Controls.Add(_lblMeta);
            Controls.Add(_tags);
            Controls.Add(_btnSelect);
            Resize += ResourceCard_Resize;
            DisposeIconToken();
        }

        private void DisposeIconToken()
        {
            // 占位，构造函数保持简洁
        }

        private void ResourceCard_Resize(object sender, EventArgs e)
        {
            _btnSelect.Location = new Point(Math.Max(68, Width - _btnSelect.Width - 14), 44);
        }

        /// <summary>绑定资源并异步加载图标（不阻塞列表渲染）。</summary>
        public void Bind(ResourceItem item)
        {
            _token++;
            int myToken = _token;
            CancelIconLoad();
            _item = item;
            if (item == null) return;

            _lblName.Text = string.IsNullOrWhiteSpace(item.Version)
                ? item.Name
                : item.Name + "  ·  " + item.Version;
            _lblDesc.Text = string.IsNullOrWhiteSpace(item.Description) ? L.Get("res.noDesc") : item.Description;
            string size = item.FileSize.HasValue ? UsbDeviceInfo.FormatSize(item.FileSize.Value) : L.Get("res.unknownSize");
            string updated = item.UpdatedAt.HasValue
                ? item.UpdatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd") : L.Get("res.unknownTime");
            _lblMeta.Text = size + "   |   " + L.Format("res.updatedOn", updated);

            RenderTags(item.Tags);
            _btnSelect.Location = new Point(Math.Max(68, Width - _btnSelect.Width - 14), 44);

            _icon.Image = IconCache.Placeholder;
            if (string.IsNullOrWhiteSpace(item.IconUrl)) return;

            _iconCts = new CancellationTokenSource();
            var ct = _iconCts.Token;
            string url = item.IconUrl;
            Task.Run(async () =>
            {
                Image img = await _icons.GetAsync(url, ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested || myToken != _token) return;
                if (img == null) return;
                Image thumb = IconCache.MakeThumbnail(img, 48);
                BeginInvoke(new Action(() =>
                {
                    if (myToken != _token || IsDisposed) { thumb.Dispose(); return; }
                    var old = _icon.Image;
                    _icon.Image = thumb;
                    if (old != null && old != IconCache.Placeholder) old.Dispose();
                }));
            }, ct);
        }

        private void RenderTags(System.Collections.Generic.List<string> tags)
        {
            _tags.Controls.Clear();
            if (tags == null) return;
            int x = 0;
            foreach (string tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var chip = new Label
                {
                    Text = tag,
                    Font = UiTheme.FontSmall,
                    ForeColor = UiTheme.AccentDark,
                    BackColor = UiTheme.HeadActive,
                    AutoSize = true,
                    Padding = new Padding(4, 1, 4, 1),
                    Margin = new Padding(0, 0, 4, 0)
                };
                _tags.Controls.Add(chip);
                x += chip.Width + 4;
                if (x > 290) break;
            }
        }

        private void CancelIconLoad()
        {
            if (_iconCts != null)
            {
                try { _iconCts.Cancel(); } catch { }
                _iconCts.Dispose();
                _iconCts = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) CancelIconLoad();
            base.Dispose(disposing);
        }
    }
}
