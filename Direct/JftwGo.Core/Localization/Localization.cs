using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace JftwGo.Core.Localization
{
    /// <summary>
    /// 本地化入口。语言包名即文件名：zh_cn / zh_tw / zh_hk。
    /// 用法：L.Get("common.ok") / L.Format("api.retryFailed", n)
    /// 语言包缺失的 key 回退到默认语言；仍未找到则返回 key 本身，便于发现遗漏。
    /// </summary>
    public static class L
    {
        private static readonly Dictionary<string, string> Strings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string _language = "zh_cn";
        private static string _fallback = "zh_cn";
        private static string _langDir;

        public static string Language { get { return _language; } }
        public static event EventHandler LanguageChanged;

        /// <summary>从程序目录下的 lang 文件夹加载语言包。exe 与 lang 同级。</summary>
        public static void Initialize(string language = null)
        {
            _langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
            if (!Directory.Exists(_langDir)) Directory.CreateDirectory(_langDir);

            if (string.IsNullOrWhiteSpace(language))
            {
                // 优先读取上次选择，否则按系统区域猜测
                string saved = ReadSavedLanguage();
                language = string.IsNullOrWhiteSpace(saved) ? GuessLanguage() : saved;
            }
            SetLanguage(language);
        }

        public static void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) language = "zh_cn";
            language = language.Trim().ToLowerInvariant().Replace('-', '_');

            LoadInto(language, Strings, false);
            if (!string.Equals(language, _fallback, StringComparison.OrdinalIgnoreCase))
                LoadInto(_fallback, Strings, true); // 回退只填缺失项

            _language = language;
            SaveLanguage(language);

            var h = LanguageChanged;
            if (h != null) h(null, EventArgs.Empty);
        }

        private static void LoadInto(string language, Dictionary<string, string> target, bool onlyMissing)
        {
            string file = Path.Combine(_langDir, language + ".yaml");
            if (!File.Exists(file)) return;
            try
            {
                var loaded = YamlReader.Parse(File.ReadAllText(file, Encoding.UTF8));
                foreach (var kv in loaded)
                {
                    if (onlyMissing)
                    {
                        if (!target.ContainsKey(kv.Key)) target[kv.Key] = kv.Value;
                    }
                    else target[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("语言包加载失败 " + file + "：" + ex.Message);
            }
        }

        public static string Get(string key)
        {
            string v;
            if (key != null && Strings.TryGetValue(key, out v)) return v;
            return key ?? "";
        }

        public static string Format(string key, params object[] args)
        {
            string fmt = Get(key);
            try { return args == null || args.Length == 0 ? fmt : string.Format(fmt, args); }
            catch { return fmt; }
        }

        /// <summary>把当前语言套用到整个控件树（含子控件）。静态文案控件用 Tag="lang:key" 标记。</summary>
        public static void ApplyTo(Control root)
        {
            if (root == null) return;
            ApplyToCore(root);
            root.Refresh();
        }

        private static void ApplyToCore(Control control)
        {
            var tag = control.Tag as string;
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith("lang:", StringComparison.OrdinalIgnoreCase))
            {
                string key = tag.Substring(5);
                control.Text = Get(key);
            }
            foreach (Control child in control.Controls) ApplyToCore(child);
        }

        private static string GuessLanguage()
        {
            var name = CultureInfo.CurrentUICulture.Name.ToLowerInvariant().Replace('-', '_');
            if (name.StartsWith("zh")) return "zh_cn";
            if (name.StartsWith("en")) return "en_us";
            return "zh_cn";
        }

        private static string SavedFile
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jftwGo", "language.txt");
            }
        }

        private static string ReadSavedLanguage()
        {
            try
            {
                return File.Exists(SavedFile) ? File.ReadAllText(SavedFile, Encoding.UTF8).Trim() : null;
            }
            catch { return null; }
        }

        private static void SaveLanguage(string language)
        {
            try
            {
                string f = SavedFile;
                string dir = Path.GetDirectoryName(f);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(f, language, Encoding.UTF8);
            }
            catch { }
        }
    }
}
