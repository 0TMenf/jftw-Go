using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LauncherGen
{
    /// <summary>实时预览：按配置绘制与生成的 start1.exe 一致的界面。</summary>
    public class PreviewControl : Control
    {
        private LauncherConfig _cfg;
        private Image _bgImage;
        private const int DW = 860, DH = 600;   // 设计尺寸（与 C 模板一致）
        private int _page = 0;

        public PreviewControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        public LauncherConfig Config
        {
            get { return _cfg; }
            set { _cfg = value; _page = 0; LoadBgImage(); Invalidate(); }
        }

        public int Page
        {
            get { return _page; }
            set { _page = value; Invalidate(); }
        }

        public int PageCount
        {
            get
            {
                if (_cfg == null || _cfg.Games.Count == 0) return 1;
                int ps = _cfg.PageSize > 0 ? _cfg.PageSize : _cfg.Games.Count;
                return (int)Math.Ceiling(_cfg.Games.Count / (double)ps);
            }
        }

        private void LoadBgImage()
        {
            _bgImage = null;
            if (_cfg != null && _cfg.BackgroundMode == "image" &&
                !string.IsNullOrEmpty(_cfg.BackgroundImagePath) && System.IO.File.Exists(_cfg.BackgroundImagePath))
            {
                try
                {
                    using (var img = Image.FromFile(_cfg.BackgroundImagePath))
                        _bgImage = (Image)img.Clone();
                }
                catch { _bgImage = null; }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_cfg == null)
            {
                g.Clear(Color.White);
                return;
            }

            // 等比缩放到控件尺寸
            float sx = (float)Width / DW, sy = (float)Height / DH;
            float s = Math.Min(sx, sy);
            int rw = (int)(DW * s), rh = (int)(DH * s);
            int ox = (Width - rw) / 2, oy = (Height - rh) / 2;

            g.Clear(Color.FromArgb(30, 30, 34));
            g.TranslateTransform(ox, oy);
            g.ScaleTransform(s, s);

            // 裁剪到设计区域
            g.SetClip(new RectangleF(0, 0, DW, DH));

            DrawLauncher(g);
        }

        private void DrawLauncher(Graphics g)
        {
            Color bg = _cfg.GetBackground();
            Color text = _cfg.GetText();
            Color card = _cfg.GetCard();
            Color accent = _cfg.GetAccent();

            // 背景
            if (_bgImage != null)
            {
                g.DrawImage(_bgImage, new RectangleF(0, 0, DW, DH));
                using (var mask = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(mask, 0, 0, DW, DH);
            }
            else
            {
                g.Clear(bg);
            }

            using (var fTitle = new Font("Microsoft YaHei", 26, FontStyle.Bold))
            using (var fSmall = new Font("Microsoft YaHei", 14))
            using (var fCard = new Font("Microsoft YaHei", 16, FontStyle.Bold))
            using (var brText = new SolidBrush(text))
            using (var brMuted = new SolidBrush(Color.FromArgb(180, text)))
            {
                g.DrawString(_cfg.Title, fTitle, brText, new PointF(28, 20));
                int pageSize = _cfg.PageSize > 0 ? _cfg.PageSize : Math.Max(1, _cfg.Games.Count);
                g.DrawString(string.Format("共 {0} 个游戏  ·  第 {1} / {2} 页  ·  点击卡片启动",
                    _cfg.Games.Count, _page + 1, PageCount), fSmall, brMuted, new PointF(30, 62));

                // 卡片网格
                const int CARD_W = 240, CARD_H = 92, GAP_X = 24, GAP_Y = 20, TOP_Y = 96, FOOT_H = 64;
                int cols = (DW - 2 * GAP_X + GAP_X) / (CARD_W + GAP_X);
                if (cols < 1) cols = 1;
                int count = Math.Min(pageSize, _cfg.Games.Count - _page * pageSize);
                if (count < 0) count = 0;
                int rowCount = (count + cols - 1) / cols;
                int gridH = rowCount * CARD_H + Math.Max(0, rowCount - 1) * GAP_Y;
                int top = TOP_Y + Math.Max(0, ((DH - TOP_Y - FOOT_H) - gridH) / 2);

                for (int i = 0; i < count; i++)
                {
                    int row = i / cols, col = i % cols;
                    int x = GAP_X + col * (CARD_W + GAP_X);
                    int y = top + row * (CARD_H + GAP_Y);
                    var rc = new Rectangle(x, y, CARD_W, CARD_H);
                    DrawRoundRect(g, rc, 14, card, Color.FromArgb(230, card), 1);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(_cfg.Games[_page * pageSize + i].Name, fCard, brText, rc, sf);
                }

                // 分页
                using (var brAccent = new SolidBrush(accent))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(string.Format("‹   {0} / {1}   ›", _page + 1, PageCount),
                        fCard, brText, new RectangleF(DW / 2 - 90, DH - FOOT_H + 10, 180, 40), sf);
                }
            }
        }

        private static void DrawRoundRect(Graphics g, Rectangle r, int radius, Color border, Color fill, int width)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(r.X, r.Y, radius, radius, 180, 90);
                path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
                path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                using (var br = new SolidBrush(fill)) g.FillPath(br, path);
                using (var pen = new Pen(border, width)) g.DrawPath(pen, path);
            }
        }
    }
}
