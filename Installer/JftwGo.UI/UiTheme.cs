using System.Drawing;
using System.Windows.Forms;

namespace JftwGo.UI
{
    /// <summary>
    /// 统一视觉风格（轻量、低干扰）。所有子程序与控件从这里取色与字体。
    /// 使用相对 DPI 的字体与 Padding，配合窗体 AutoScaleMode=Dpi 适配高 DPI。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Accent = Color.FromArgb(0x2C, 0x7D, 0xCF);
        public static readonly Color AccentDark = Color.FromArgb(0x1F, 0x5E, 0x9E);
        public static readonly Color Danger = Color.FromArgb(0xC0, 0x39, 0x2B);
        public static readonly Color Success = Color.FromArgb(0x27, 0x8A, 0x4E);
        public static readonly Color TextPrimary = Color.FromArgb(0x26, 0x2A, 0x2E);
        public static readonly Color TextSecondary = Color.FromArgb(0x70, 0x77, 0x7E);
        public static readonly Color Surface = Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static readonly Color Page = Color.FromArgb(0xF5, 0xF6, 0xF8);
        public static readonly Color CardBorder = Color.FromArgb(0xE2, 0xE5, 0xE9);
        public static readonly Color HeadActive = Color.FromArgb(0xEC, 0xF3, 0xFB);

        // 全部使用内嵌思源黑体（SIL OFL），不使用微软雅黑/Consolas 等有商用授权的字体。
        public static Font FontNormal { get { return FontLoader.Create(9F); } }
        public static Font FontTitle { get { return FontLoader.Create(15F, FontStyle.Bold); } }
        public static Font FontSubTitle { get { return FontLoader.Create(11F); } }
        public static Font FontSmall { get { return FontLoader.Create(8.5F); } }
        public static Font FontButton { get { return FontLoader.Create(9F); } }
        public static Font FontMono { get { return FontLoader.Create(9F); } }

        /// <summary>把窗体配置为高 DPI 友好的样式。</summary>
        public static void PrepareForm(Form form)
        {
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Font = FontNormal;
            form.BackColor = Page;
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        public static Button MakeButton(string text, bool primary)
        {
            var b = new Button
            {
                Text = text,
                Font = FontButton,
                Height = 32,
                MinimumSize = new Size(88, 32),
                Padding = new Padding(10, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 1;
            if (primary)
            {
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor = AccentDark;
            }
            else
            {
                b.BackColor = Surface;
                b.ForeColor = TextPrimary;
                b.FlatAppearance.BorderColor = CardBorder;
            }
            return b;
        }
    }
}
