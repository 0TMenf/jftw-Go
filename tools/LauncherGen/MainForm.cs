using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace LauncherGen
{
    public class MainForm : Form
    {
        private LauncherConfig _cfg = LauncherConfig.Demo();
        private readonly PreviewControl _preview = new PreviewControl();

        // 控件
        private TextBox _txtTitle, _txtAccent, _txtBgColor, _txtTextColor, _txtCardColor, _txtBgImage;
        private ComboBox _cmbTheme, _cmbBgMode;
        private NumericUpDown _numPageSize;
        private CheckBox _chkKillJiYu;
        private ListBox _lstGames;
        private Button _btnAdd, _btnEdit, _btnRemove, _btnUp, _btnDown;
        private Button _btnBrowseBg, _btnGenerate, _btnSave, _btnLoad, _btnPrev, _btnNext;
        private Label _lblStatus;

        private const string ConfigFile = "launcher-config.json";

        public MainForm()
        {
            Text = "jftw Go · start1.exe 生成器";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei", 9F);
            BackColor = Color.FromArgb(245, 246, 250);

            BuildUI();
            LoadConfigIfExists();
            BindConfigToUI();
            RefreshPreview();
        }

        private void BuildUI()
        {
            // ---------- 左侧配置面板 ----------
            var left = new Panel { Dock = DockStyle.Left, Width = 520, Padding = new Padding(14) };

            int y = 8;

            // 标题
            left.Controls.Add(new Label { Text = "窗口标题", Location = new Point(14, y + 3), AutoSize = true });
            _txtTitle = new TextBox { Location = new Point(100, y), Size = new Size(390, 26) };
            _txtTitle.TextChanged += (s, e) => { _cfg.Title = _txtTitle.Text; RefreshPreview(); };
            left.Controls.Add(_txtTitle);
            y += 38;

            // 主题分组
            var grpTheme = new GroupBox { Text = "颜色主题", Location = new Point(8, y), Size = new Size(490, 118) };
            grpTheme.Controls.Add(new Label { Text = "预设", Location = new Point(12, 26), AutoSize = true });
            _cmbTheme = new ComboBox { Location = new Point(60, 22), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTheme.Items.AddRange(new object[] { "dark（深色）", "light（浅色）", "custom（自定义）" });
            _cmbTheme.SelectedIndexChanged += (s, e) =>
            {
                _cfg.Theme = _cmbTheme.SelectedIndex == 0 ? "dark" : (_cmbTheme.SelectedIndex == 1 ? "light" : "custom");
                UpdateColorFieldsEnabled();
                RefreshPreview();
            };
            grpTheme.Controls.Add(_cmbTheme);

            grpTheme.Controls.Add(new Label { Text = "主题色", Location = new Point(12, 58), AutoSize = true });
            _txtAccent = new TextBox { Location = new Point(60, 55), Size = new Size(90, 24) };
            _txtAccent.TextChanged += (s, e) => { _cfg.AccentColor = _txtAccent.Text; RefreshPreview(); };
            grpTheme.Controls.Add(_txtAccent);
            grpTheme.Controls.Add(new Label { Text = "背景", Location = new Point(170, 58), AutoSize = true });
            _txtBgColor = new TextBox { Location = new Point(218, 55), Size = new Size(90, 24) };
            _txtBgColor.TextChanged += (s, e) => { _cfg.BackgroundColor = _txtBgColor.Text; RefreshPreview(); };
            grpTheme.Controls.Add(_txtBgColor);
            grpTheme.Controls.Add(new Label { Text = "文字", Location = new Point(326, 58), AutoSize = true });
            _txtTextColor = new TextBox { Location = new Point(364, 55), Size = new Size(90, 24) };
            _txtTextColor.TextChanged += (s, e) => { _cfg.TextColor = _txtTextColor.Text; RefreshPreview(); };
            grpTheme.Controls.Add(_txtTextColor);
            grpTheme.Controls.Add(new Label { Text = "卡片", Location = new Point(12, 88), AutoSize = true });
            _txtCardColor = new TextBox { Location = new Point(60, 85), Size = new Size(90, 24) };
            _txtCardColor.TextChanged += (s, e) => { _cfg.CardColor = _txtCardColor.Text; RefreshPreview(); };
            grpTheme.Controls.Add(_txtCardColor);
            grpTheme.Controls.Add(new Label { Text = "格式：RRGGBB，如 4F8CFF", Location = new Point(170, 88), AutoSize = true, ForeColor = Color.Gray });
            left.Controls.Add(grpTheme);
            y += 128;

            // 背景分组
            var grpBg = new GroupBox { Text = "背景", Location = new Point(8, y), Size = new Size(490, 92) };
            grpBg.Controls.Add(new Label { Text = "模式", Location = new Point(12, 26), AutoSize = true });
            _cmbBgMode = new ComboBox { Location = new Point(60, 22), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbBgMode.Items.AddRange(new object[] { "solid（纯色）", "image（图片）" });
            _cmbBgMode.SelectedIndexChanged += (s, e) =>
            {
                _cfg.BackgroundMode = _cmbBgMode.SelectedIndex == 0 ? "solid" : "image";
                UpdateBgFieldsEnabled();
                RefreshPreview();
            };
            grpBg.Controls.Add(_cmbBgMode);
            grpBg.Controls.Add(new Label { Text = "图片", Location = new Point(12, 58), AutoSize = true });
            _txtBgImage = new TextBox { Location = new Point(60, 55), Size = new Size(280, 24), ReadOnly = true };
            grpBg.Controls.Add(_txtBgImage);
            _btnBrowseBg = new Button { Text = "浏览…", Location = new Point(348, 53), Size = new Size(120, 28) };
            _btnBrowseBg.Click += BrowseBg;
            grpBg.Controls.Add(_btnBrowseBg);
            left.Controls.Add(grpBg);
            y += 102;

            // 分页 + 极域
            var grpOpt = new GroupBox { Text = "选项", Location = new Point(8, y), Size = new Size(490, 66) };
            grpOpt.Controls.Add(new Label { Text = "每页游戏数", Location = new Point(12, 28), AutoSize = true });
            _numPageSize = new NumericUpDown { Location = new Point(100, 25), Size = new Size(90, 26), Minimum = 1, Maximum = 100, Value = 8 };
            _numPageSize.ValueChanged += (s, e) => { _cfg.PageSize = (int)_numPageSize.Value; RefreshPreview(); };
            grpOpt.Controls.Add(_numPageSize);
            _chkKillJiYu = new CheckBox { Text = "启动时结束极域进程", Location = new Point(220, 27), AutoSize = true };
            _chkKillJiYu.CheckedChanged += (s, e) => { _cfg.KillJiYu = _chkKillJiYu.Checked; };
            grpOpt.Controls.Add(_chkKillJiYu);
            left.Controls.Add(grpOpt);
            y += 76;

            // 游戏列表
            var grpGames = new GroupBox { Text = "游戏列表（exe / bat / sh）", Location = new Point(8, y), Size = new Size(490, 320) };
            _lstGames = new ListBox { Location = new Point(12, 26), Size = new Size(460, 210) };
            _lstGames.SelectedIndexChanged += (s, e) => RefreshPreview();
            _lstGames.DoubleClick += (s, e) => EditGame();
            grpGames.Controls.Add(_lstGames);

            _btnAdd = new Button { Text = "添加文件…", Location = new Point(12, 246), Size = new Size(110, 30) };
            _btnAdd.Click += AddGames;
            _btnEdit = new Button { Text = "编辑", Location = new Point(130, 246), Size = new Size(80, 30) };
            _btnEdit.Click += (s, e) => EditGame();
            _btnRemove = new Button { Text = "删除", Location = new Point(218, 246), Size = new Size(80, 30) };
            _btnRemove.Click += RemoveGame;
            _btnUp = new Button { Text = "↑", Location = new Point(306, 246), Size = new Size(40, 30) };
            _btnUp.Click += (s, e) => MoveGame(-1);
            _btnDown = new Button { Text = "↓", Location = new Point(352, 246), Size = new Size(40, 30) };
            _btnDown.Click += (s, e) => MoveGame(1);
            grpGames.Controls.Add(_btnAdd);
            grpGames.Controls.Add(_btnEdit);
            grpGames.Controls.Add(_btnRemove);
            grpGames.Controls.Add(_btnUp);
            grpGames.Controls.Add(_btnDown);
            grpGames.Controls.Add(new Label { Text = "双击条目编辑名称/参数/工作目录", Location = new Point(12, 282), AutoSize = true, ForeColor = Color.Gray });
            left.Controls.Add(grpGames);
            y += 330;

            // 底部操作按钮
            _btnSave = new Button { Text = "保存配置", Location = new Point(12, y), Size = new Size(110, 34) };
            _btnSave.Click += SaveConfig;
            _btnLoad = new Button { Text = "载入配置", Location = new Point(130, y), Size = new Size(110, 34) };
            _btnLoad.Click += (s, e) => { LoadConfigIfExists(); BindConfigToUI(); RefreshPreview(); };
            left.Controls.Add(_btnSave);
            left.Controls.Add(_btnLoad);

            _btnGenerate = new Button
            {
                Text = "生成 start1.exe",
                Location = new Point(258, y),
                Size = new Size(230, 34),
                BackColor = Color.FromArgb(79, 140, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnGenerate.FlatAppearance.BorderSize = 0;
            _btnGenerate.Click += Generate;
            left.Controls.Add(_btnGenerate);

            // ---------- 右侧预览 ----------
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 14, 18, 14) };
            right.Controls.Add(new Label
            {
                Text = "实时预览（与生成效果一致）",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold)
            });

            var previewBorder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(210, 214, 224),
                Padding = new Padding(2)
            };
            _preview.Dock = DockStyle.Fill;
            previewBorder.Controls.Add(_preview);
            right.Controls.Add(previewBorder);

            // 翻页控制（预览）
            var navPanel = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            _btnPrev = new Button { Text = "◀ 上一页", Location = new Point(10, 8), Size = new Size(110, 30) };
            _btnPrev.Click += (s, e) => { if (_preview.Page > 0) { _preview.Page--; UpdateNav(); } };
            _btnNext = new Button { Text = "下一页 ▶", Location = new Point(130, 8), Size = new Size(110, 30) };
            _btnNext.Click += (s, e) => { if (_preview.Page < _preview.PageCount - 1) { _preview.Page++; UpdateNav(); } };
            _lblStatus = new Label { Location = new Point(260, 15), AutoSize = true, ForeColor = Color.Gray };
            navPanel.Controls.Add(_btnPrev);
            navPanel.Controls.Add(_btnNext);
            navPanel.Controls.Add(_lblStatus);
            right.Controls.Add(navPanel);

            Controls.Add(right);
            Controls.Add(left);
        }

        private void UpdateColorFieldsEnabled()
        {
            bool custom = _cfg.Theme == "custom";
            _txtAccent.Enabled = _txtBgColor.Enabled = _txtTextColor.Enabled = _txtCardColor.Enabled = custom;
        }

        private void UpdateBgFieldsEnabled()
        {
            bool image = _cfg.BackgroundMode == "image";
            _txtBgImage.Enabled = _btnBrowseBg.Enabled = image;
        }

        private void BindConfigToUI()
        {
            _txtTitle.Text = _cfg.Title;
            _cmbTheme.SelectedIndex = _cfg.Theme == "light" ? 1 : (_cfg.Theme == "custom" ? 2 : 0);
            _cmbBgMode.SelectedIndex = _cfg.BackgroundMode == "image" ? 1 : 0;
            _txtAccent.Text = _cfg.AccentColor;
            _txtBgColor.Text = _cfg.BackgroundColor;
            _txtTextColor.Text = _cfg.TextColor;
            _txtCardColor.Text = _cfg.CardColor;
            _txtBgImage.Text = _cfg.BackgroundImagePath;
            _numPageSize.Value = Math.Max(1, Math.Min(100, _cfg.PageSize));
            _chkKillJiYu.Checked = _cfg.KillJiYu;
            RefreshGameList();
            UpdateColorFieldsEnabled();
            UpdateBgFieldsEnabled();
            UpdateNav();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            _preview.Config = _cfg;
            UpdateNav();
        }

        private void UpdateNav()
        {
            if (_lblStatus == null) return;
            _lblStatus.Text = string.Format("游戏 {0} 个  ·  共 {1} 页  ·  当前第 {2} 页",
                _cfg.Games.Count, _preview.PageCount, _preview.Page + 1);
            _btnPrev.Enabled = _preview.Page > 0;
            _btnNext.Enabled = _preview.Page < _preview.PageCount - 1;
        }

        private void RefreshGameList()
        {
            _lstGames.Items.Clear();
            foreach (var g in _cfg.Games)
                _lstGames.Items.Add(string.Format("{0}   [{1}]", g.Name, g.RelativePath));
            if (_cfg.Games.Count > 0) _lstGames.SelectedIndex = 0;
        }

        // ---------- 游戏编辑 ----------
        private void AddGames(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Title = "选择游戏程序（exe / bat / sh）",
                Filter = "游戏程序|*.exe;*.bat;*.cmd;*.sh|所有文件|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            foreach (var file in ofd.FileNames)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                var item = new GameItem
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    RelativePath = file,   // 先存绝对路径，生成时转相对
                    WorkDir = Path.GetDirectoryName(file)
                };
                _cfg.Games.Add(item);
            }
            RefreshGameList();
            RefreshPreview();
        }

        private void EditGame()
        {
            int idx = _lstGames.SelectedIndex;
            if (idx < 0 || idx >= _cfg.Games.Count) return;
            var dlg = new GameEditForm(_cfg.Games[idx].Clone());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _cfg.Games[idx] = dlg.Result;
                RefreshGameList();
                RefreshPreview();
            }
        }

        private void RemoveGame(object sender, EventArgs e)
        {
            int idx = _lstGames.SelectedIndex;
            if (idx < 0) return;
            _cfg.Games.RemoveAt(idx);
            RefreshGameList();
            RefreshPreview();
        }

        private void MoveGame(int delta)
        {
            int idx = _lstGames.SelectedIndex;
            int target = idx + delta;
            if (idx < 0 || target < 0 || target >= _cfg.Games.Count) return;
            var tmp = _cfg.Games[idx];
            _cfg.Games[idx] = _cfg.Games[target];
            _cfg.Games[target] = tmp;
            RefreshGameList();
            _lstGames.SelectedIndex = target;
            RefreshPreview();
        }

        private void BrowseBg(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            _cfg.BackgroundImagePath = ofd.FileName;
            _txtBgImage.Text = ofd.FileName;
            RefreshPreview();
        }

        // ---------- 配置持久化 ----------
        private string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFile); }
        }

        private void SaveConfig(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented), Encoding.UTF8);
                MessageBox.Show(this, "配置已保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadConfigIfExists()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    _cfg = JsonConvert.DeserializeObject<LauncherConfig>(File.ReadAllText(ConfigPath, Encoding.UTF8)) ?? LauncherConfig.Demo();
            }
            catch { _cfg = LauncherConfig.Demo(); }
        }

        // ---------- 生成 ----------
        private void Generate(object sender, EventArgs e)
        {
            // 选择输出目录
            var fbd = new FolderBrowserDialog
            {
                Description = "选择输出目录（生成的 start1.exe 与背景图片将放在这里）"
            };
            if (fbd.ShowDialog(this) != DialogResult.OK) return;
            string outDir = fbd.SelectedPath;

            try
            {
                // 1) 处理背景图片：复制到输出目录
                string bgName = "";
                if (_cfg.BackgroundMode == "image" && !string.IsNullOrEmpty(_cfg.BackgroundImagePath)
                    && File.Exists(_cfg.BackgroundImagePath))
                {
                    bgName = "launcher-bg" + Path.GetExtension(_cfg.BackgroundImagePath).ToLowerInvariant();
                    File.Copy(_cfg.BackgroundImagePath, Path.Combine(outDir, bgName), true);
                }

                // 2) 把游戏路径转成相对路径（相对于输出目录）
                var cfgForGen = CloneConfig(_cfg);
                foreach (var g in cfgForGen.Games)
                {
                    g.RelativePath = ToRelativePublic(outDir, g.RelativePath);
                    if (!string.IsNullOrEmpty(g.WorkDir) && Path.IsPathRooted(g.WorkDir))
                        g.WorkDir = ToRelativePublic(outDir, g.WorkDir);
                }

                // 3) 生成 C 源码
                string csrc = CodeEmitter.Emit(cfgForGen, bgName);
                string cPath = Path.Combine(outDir, "start1.c");
                File.WriteAllText(cPath, csrc, new UTF8Encoding(false));

                // 4) 调用 zig 编译
                string zig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "ziglang", "ziglang", "zig.exe");
                zig = Path.GetFullPath(zig);
                if (!File.Exists(zig))
                {
                    MessageBox.Show(this,
                        "已生成 C 源码：\n" + cPath + "\n\n但未找到 zig 编译器（tools\\ziglang\\ziglang\\zig.exe）。\n请用 GCC/MSVC 手动编译，或放置 zig.exe。",
                        "生成完成（未编译）", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = zig,
                    Arguments = string.Format("cc -target x86_64-windows-gnu -O2 -municode \"{0}\" -o \"{1}\" -lgdiplus -lshell32 -luser32 -lgdi32 -lm",
                        cPath, Path.Combine(outDir, "start1.exe")),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = outDir
                };
                var proc = System.Diagnostics.Process.Start(psi);
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                    MessageBox.Show(this,
                        "生成成功！\n\nstart1.exe：" + Path.Combine(outDir, "start1.exe") +
                        "\nC 源码：" + cPath +
                        (_cfg.BackgroundMode == "image" ? "\n背景图片：" + Path.Combine(outDir, bgName) : ""),
                        "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, "编译失败：\n" + err, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "生成失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static LauncherConfig CloneConfig(LauncherConfig src)
        {
            var c = new LauncherConfig
            {
                Title = src.Title, Theme = src.Theme,
                AccentColor = src.AccentColor, BackgroundColor = src.BackgroundColor,
                TextColor = src.TextColor, CardColor = src.CardColor,
                BackgroundMode = src.BackgroundMode, BackgroundImagePath = src.BackgroundImagePath,
                PageSize = src.PageSize, KillJiYu = src.KillJiYu
            };
            foreach (var g in src.Games) c.Games.Add(g.Clone());
            return c;
        }

        public static string ToRelativePublic(string baseDir, string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (!Path.IsPathRooted(path)) return path;
            try
            {
                var baseUri = new Uri(TrimEndSep(baseDir) + Path.DirectorySeparatorChar);
                var pathUri = new Uri(path);
                string rel = Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString());
                return rel.Replace('/', '\\');
            }
            catch { return path; }
        }

        private static string TrimEndSep(string p)
        {
            return p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
