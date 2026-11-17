using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JftwGo.Core;
using JftwGo.Core.Download;
using JftwGo.Core.Packaging;
using JftwGo.UI;
using JftwGo.UI.Controls;
using JftwGo.UI.Wizard;

namespace JftwGo.Direct.Pages
{
    /// <summary>下载与安装进度页：下载 → 校验 → 解压 → 可选启动，支持取消与重试。</summary>
    public class DownloadInstallPage : WizardPage
    {
        private readonly DirectContext _ctx;
        private readonly ProgressPanel _progress;
        private readonly LogPanel _log;
        private readonly Button _btnRetry;
        private CancellationTokenSource _cts;
        private bool _started;
        private bool _finished;

        public Action OnFinished;

        public override string PageTitle { get { return "下载并安装"; } }
        public override bool CanGoNext { get { return _finished; } }

        public DownloadInstallPage(DirectContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(20) };

            _progress = new ProgressPanel { Dock = DockStyle.Top, Height = 78 };
            _log = new LogPanel { Dock = DockStyle.Bottom, Height = 28 };
            _log.Attach(ctx.Logger);

            var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            _btnRetry = UiTheme.MakeButton("重试", false);
            _btnRetry.Location = new Point(0, 4);
            _btnRetry.Enabled = false;
            _btnRetry.Click += (s, e) => { _ = RunAsync(); };
            var btnCancel = UiTheme.MakeButton("取消", false);
            btnCancel.Location = new Point(96, 4);
            btnCancel.Click += (s, e) => CancelWork();
            toolbar.Controls.Add(_btnRetry);
            toolbar.Controls.Add(btnCancel);

            card.Controls.Add(_log);
            card.Controls.Add(toolbar);
            card.Controls.Add(_progress);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            if (!_started)
            {
                _started = true;
                var ignore = RunAsync();
            }
            if (Owner != null) Owner.UpdateNextButton();
        }

        private void CancelWork()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _ctx.Logger.Warn("用户取消了安装流程。");
            }
        }

        private async Task RunAsync()
        {
            if (_cts != null) { try { _cts.Cancel(); } catch { } }
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _finished = false;
            _btnRetry.Enabled = false;
            if (Owner != null) { Owner.SetNextEnabled(false); Owner.UpdateNextButton(); }

            var item = _ctx.SelectedResource;
            if (item == null)
            {
                ShowFail("未选择资源，无法安装。");
                return;
            }
            if (string.IsNullOrWhiteSpace(_ctx.InstallDir))
            {
                ShowFail("未指定安装目录。");
                return;
            }

            try
            {
                _progress.SetProgress(0, "开始下载资源…", "");
                string package = Path.Combine(AppPaths.DownloadDir,
                    Sanitize(item.Name) + "_" + (item.Version ?? "v") + ".pkg");

                var downloader = new Downloader(_ctx.Logger);
                var dl = new Progress<DownloadProgressInfo>(p =>
                    _progress.SetProgress(p.Percent,
                        p.StatusText ?? ("正在下载… " + p.FormatSize(p.BytesReceived)),
                        p.SpeedBytesPerSec > 0 ? FormatSpeed(p.SpeedBytesPerSec) : ""));

                var result = await downloader.DownloadAsync(item, package, dl, ct).ConfigureAwait(true);
                if (!result.Success)
                {
                    ShowFail(result.ErrorMessage);
                    return;
                }

                ct.ThrowIfCancellationRequested();

                // 解压/安装
                _progress.SetProgress(0, "正在安装到目标目录…", "");
                string dir = _ctx.InstallDir;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (ArchiveExtractor.IsZip(package))
                {
                    var ex = new Progress<int>(v =>
                        _progress.SetProgress(v, "正在解压安装…", ""));
                    await ArchiveExtractor.ExtractZipAsync(package, dir, ex, ct).ConfigureAwait(true);
                }
                else
                {
                    ArchiveExtractor.CopyFileToDir(package, dir);
                    _progress.SetProgress(100, "已复制资源文件。", "");
                }

                _ctx.LaunchExePath = FindLauncher(dir);
                _progress.SetProgress(100, "安装完成。", "");

                if (_ctx.LaunchAfterInstall)
                    TryLaunch(_ctx.LaunchExePath);

                _finished = true;
                if (Owner != null) { Owner.SetNextEnabled(true); Owner.UpdateNextButton(); }
                if (OnFinished != null) OnFinished();
            }
            catch (OperationCanceledException)
            {
                _progress.SetProgress(0, "已取消。", "");
                if (Owner != null) { Owner.SetNextEnabled(false); Owner.UpdateNextButton(); }
            }
            catch (Exception ex)
            {
                ShowFail("安装失败：" + ex.Message);
            }
        }

        private void TryLaunch(string exe)
        {
            try
            {
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                {
                    _ctx.Logger.Warn("未找到可启动程序，已跳过启动。");
                    return;
                }
                Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
                _ctx.Logger.Info("已启动：" + exe);
            }
            catch (Exception ex)
            {
                _ctx.Logger.Warn("启动失败：" + ex.Message);
            }
        }

        /// <summary>在安装目录中寻找最可能的启动程序。</summary>
        private static string FindLauncher(string dir)
        {
            try
            {
                var exes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
                if (exes.Length == 0)
                    exes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
                if (exes.Length == 0) return null;

                string self = Path.GetFileName(Application.ExecutablePath);
                var candidates = exes.Where(e =>
                    !string.Equals(Path.GetFileName(e), self, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (candidates.Length == 0) return null;

                var jftw = candidates.FirstOrDefault(e =>
                    Path.GetFileName(e).IndexOf("jftw", StringComparison.OrdinalIgnoreCase) >= 0);
                if (jftw != null) return jftw;
                return candidates[0];
            }
            catch { return null; }
        }

        private void ShowFail(string message)
        {
            _ctx.Logger.Error(message);
            _progress.SetProgress(0, "失败：" + message, "");
            _btnRetry.Enabled = true;
            _finished = false;
            if (Owner != null) { Owner.SetNextEnabled(false); Owner.UpdateNextButton(); }
            WizardForm.ShowError(FindForm(), message);
        }

        private static string FormatSpeed(double bps)
        {
            if (bps < 1024) return bps.ToString("F0") + " B/s";
            if (bps < 1024 * 1024) return (bps / 1024).ToString("F1") + " KB/s";
            return (bps / (1024 * 1024)).ToString("F2") + " MB/s";
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "resource";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }
    }
}
