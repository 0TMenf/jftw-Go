using System;
using System.Collections.Generic;
using System.Drawing;

namespace LauncherGen
{
    /// <summary>一个游戏条目。相对路径相对于生成的 start1.exe 所在目录。</summary>
    public class GameItem
    {
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public string Arguments { get; set; }
        public string WorkDir { get; set; }

        public GameItem()
        {
            Name = "";
            RelativePath = "";
            Arguments = "";
            WorkDir = "";
        }

        public GameItem Clone()
        {
            return new GameItem
            {
                Name = Name,
                RelativePath = RelativePath,
                Arguments = Arguments,
                WorkDir = WorkDir
            };
        }
    }

    /// <summary>启动器生成配置。</summary>
    public class LauncherConfig
    {
        // 窗口标题
        public string Title { get; set; }

        // 主题：亮色 / 暗色 / 自定义
        public string Theme { get; set; }

        // 自定义主题颜色（十六进制 RRGGBB，不带 #）
        public string AccentColor { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string CardColor { get; set; }

        // 背景模式：solid（纯色） / image（图片）
        public string BackgroundMode { get; set; }
        // 背景图片绝对路径（配置时），生成时复制到输出目录
        public string BackgroundImagePath { get; set; }

        // 分页大小：每页显示多少个游戏（0 表示不分页，全都显示）
        public int PageSize { get; set; }

        // 是否启用"启动时结束极域进程"
        public bool KillJiYu { get; set; }

        public List<GameItem> Games { get; set; }

        public LauncherConfig()
        {
            Title = "jftw Go 游戏启动器";
            Theme = "dark";
            AccentColor = "4F8CFF";
            BackgroundColor = "121826";
            TextColor = "FFFFFF";
            CardColor = "1E2940";
            BackgroundMode = "solid";
            BackgroundImagePath = "";
            PageSize = 8;
            KillJiYu = true;
            Games = new List<GameItem>();
        }

        public static LauncherConfig Demo()
        {
            var c = new LauncherConfig();
            c.Games.Add(new GameItem { Name = "Minecraft（HMCL）", RelativePath = "Minecraft\\启动游戏双击我.bat", WorkDir = "Minecraft" });
            c.Games.Add(new GameItem { Name = "植物大战僵尸", RelativePath = "PVZ\\PlantsVsZombies.exe", WorkDir = "PVZ" });
            c.Games.Add(new GameItem { Name = "PVZ 杂交版", RelativePath = "PVZ杂交版\\植物大战僵尸杂交版发布版0.27.0.Csharp.exe", WorkDir = "PVZ杂交版" });
            c.Games.Add(new GameItem { Name = "穿越迷雾", RelativePath = "TTF穿越迷雾\\Through The Fog!.exe", WorkDir = "TTF穿越迷雾" });
            c.Games.Add(new GameItem { Name = "Undertale", RelativePath = "Undertale\\UNDERTALE.exe", WorkDir = "Undertale" });
            c.Games.Add(new GameItem { Name = "PVZ 修改器", RelativePath = "傻子PVZ修改器\\PvZ_Toolkit_v1.22.0_ZH.exe", WorkDir = "傻子PVZ修改器" });
            c.Games.Add(new GameItem { Name = "图吧工具箱", RelativePath = "图吧工具箱\\图吧工具箱2026.exe", WorkDir = "图吧工具箱" });
            c.Games.Add(new GameItem { Name = "米塔 MiSide", RelativePath = "米塔MiSide\\MiSideFull.exe", WorkDir = "米塔MiSide" });
            return c;
        }

        /// <summary>根据主题返回实际生效的配色。</summary>
        public Color GetBackground()
        {
            if (Theme == "light") return Color.FromArgb(0xF2, 0xF4, 0xF8);
            if (Theme == "custom") return Hex(BackgroundColor, Color.FromArgb(0x12, 0x18, 0x26));
            return Color.FromArgb(0x12, 0x18, 0x26); // dark
        }

        public Color GetText()
        {
            if (Theme == "light") return Color.FromArgb(0x1A, 0x1A, 0x1A);
            if (Theme == "custom") return Hex(TextColor, Color.White);
            return Color.White;
        }

        public Color GetCard()
        {
            if (Theme == "light") return Color.White;
            if (Theme == "custom") return Hex(CardColor, Color.FromArgb(0x1E, 0x29, 0x40));
            return Color.FromArgb(0x1E, 0x29, 0x40);
        }

        public Color GetAccent()
        {
            if (Theme == "light") return Color.FromArgb(0x2B, 0x6C, 0xE0);
            if (Theme == "custom") return Hex(AccentColor, Color.FromArgb(0x4F, 0x8C, 0xFF));
            return Color.FromArgb(0x4F, 0x8C, 0xFF);
        }

        private static Color Hex(string s, Color fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            s = s.Trim().TrimStart('#');
            if (s.Length == 6)
            {
                try
                {
                    return Color.FromArgb(
                        Convert.ToInt32(s.Substring(0, 2), 16),
                        Convert.ToInt32(s.Substring(2, 2), 16),
                        Convert.ToInt32(s.Substring(4, 2), 16));
                }
                catch { return fallback; }
            }
            return fallback;
        }
    }
}
