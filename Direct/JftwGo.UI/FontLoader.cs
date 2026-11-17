using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace JftwGo.UI
{
    /// <summary>
    /// 内嵌字体加载器：从程序目录 fonts\ 加载思源黑体（Noto Sans SC，SIL OFL 1.1）。
    /// 使用 GDI PrivateFontCollection 私有加载，无需系统安装，规避微软雅黑的商用授权风险。
    /// 字体缺失/损坏时优雅回退到系统 sans 字体，保证程序仍可运行。
    /// </summary>
    public static class FontLoader
    {
        public const string FontFileName = "NotoSansSC-Regular.ttf";

        private static readonly PrivateFontCollection Collection = new PrivateFontCollection();
        private static FontFamily _family;
        private static bool _initialized;
        private static readonly object Sync = new object();

        /// <summary>思源黑体字体家族；加载失败时为 null。</summary>
        public static FontFamily Family
        {
            get { EnsureLoaded(); return _family; }
        }

        public static bool IsEmbeddedFontAvailable
        {
            get { EnsureLoaded(); return _family != null; }
        }

        private static void EnsureLoaded()
        {
            if (_initialized) return;
            lock (Sync)
            {
                if (_initialized) return;
                _initialized = true;

                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts");
                    string path = Path.Combine(dir, FontFileName);
                    if (!File.Exists(path)) return;
                    Collection.AddFontFile(path);
                    if (Collection.Families.Length > 0)
                        _family = Collection.Families[0];
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("内嵌字体加载失败：" + ex.Message);
                    _family = null;
                }
            }
        }

        /// <summary>
        /// 创建指定字号/样式的字体：优先用思源黑体；
        /// 粗体若内嵌字体无对应字形，由 GDI 合成。失败回退系统 sans 字体。
        /// </summary>
        public static Font Create(float size, FontStyle style = FontStyle.Regular)
        {
            FontFamily fam = Family;
            if (fam != null)
            {
                try { return new Font(fam, size, style, GraphicsUnit.Point); }
                catch { }
            }
            // 回退：按系统存在的中文字体依次尝试
            string[] fallbacks = { "Microsoft YaHei", "SimHei", "SimSun" };
            foreach (string name in fallbacks)
            {
                try
                {
                    if (FontFamily.Families != null &&
                        Array.Exists(FontFamily.Families, f => f.Name == name))
                        return new Font(name, size, style, GraphicsUnit.Point);
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }
    }
}
