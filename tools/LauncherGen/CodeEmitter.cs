using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace LauncherGen
{
    /// <summary>把 LauncherConfig 发射为自绘 Win32 C 源码（纯 C，无运行时依赖）。</summary>
    public static class CodeEmitter
    {
        private static string C(string hex)
        {
            hex = (hex ?? "000000").Trim().TrimStart('#');
            if (hex.Length != 6) hex = "000000";
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return "RGB(" + r + "," + g + "," + b + ")";
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ToHex(Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string LoadTemplate()
        {
            // 模板作为嵌入资源；回退到 exe 同目录文件
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream("LauncherGen.template.c.txt"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                        return reader.ReadToEnd();
                }
            }
            string side = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.c.txt");
            if (File.Exists(side)) return File.ReadAllText(side, Encoding.UTF8);
            throw new FileNotFoundException("找不到 C 模板 template.c.txt");
        }

        /// <summary>生成 C 源码字符串。bgImageName: 背景图片复制到 exe 目录后的文件名，可为空。</summary>
        public static string Emit(LauncherConfig cfg, string bgImageName)
        {
            string tpl = LoadTemplate();

            bool hasImage = cfg.BackgroundMode == "image" && !string.IsNullOrEmpty(bgImageName);
            int pageSize = cfg.PageSize > 0 ? cfg.PageSize : Math.Max(1, cfg.Games.Count);

            var games = new StringBuilder();
            foreach (var g in cfg.Games)
            {
                games.Append("    { L\"").Append(Escape(g.Name)).Append("\", L\"").Append(Escape(g.RelativePath)).Append("\", ");
                games.Append(string.IsNullOrEmpty(g.Arguments) ? "NULL" : "L\"" + Escape(g.Arguments) + "\"");
                games.Append(", ");
                games.Append(string.IsNullOrEmpty(g.WorkDir) ? "NULL" : "L\"" + Escape(g.WorkDir) + "\"");
                games.AppendLine(" },");
            }
            if (cfg.Games.Count == 0) games.AppendLine("    { L\"(no games)\", L\"\", NULL, NULL },");

            tpl = tpl.Replace("@@APP_TITLE@@", "L\"" + Escape(cfg.Title) + "\"");
            tpl = tpl.Replace("@@COLOR_ACCENT@@", C(ToHex(cfg.GetAccent())));
            tpl = tpl.Replace("@@COLOR_BG@@", C(ToHex(cfg.GetBackground())));
            tpl = tpl.Replace("@@COLOR_TEXT@@", C(ToHex(cfg.GetText())));
            tpl = tpl.Replace("@@COLOR_CARD@@", C(ToHex(cfg.GetCard())));
            tpl = tpl.Replace("@@PAGE_SIZE@@", pageSize.ToString(CultureInfo.InvariantCulture));
            tpl = tpl.Replace("@@BG_IMAGE@@", hasImage ? "L\"" + Escape(bgImageName) + "\"" : "NULL");
            tpl = tpl.Replace("@@KILL_JIYU@@", cfg.KillJiYu ? "1" : "0");
            tpl = tpl.Replace("@@GAMES@@", games.ToString().TrimEnd('\r', '\n'));

            return tpl;
        }
    }
}
