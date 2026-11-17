using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JftwGo.Core;
using JftwGo.Core.Download;
using JftwGo.Core.Usb;
using JftwGo.UI;
using JftwGo.UI.Controls;
using JftwGo.UI.Wizard;

namespace JftwGo.Installer.Pages
{
    /// <summary>制作进度页：下载 → 校验 → 清盘 → 写入，含进度条、日志、取消、失败重试。</summary>
    public class WriteProgressPage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly ProgressPanel _progress;
        private readonly LogPanel _log;
        private readonly Button _btnRetry;
        private CancellationTokenSource _cts;
        private bool _started;
        private bool _finished;
        private string _packageFile;

        public Action OnFinished;

        public override string PageTitle { get { return "正在制作 U 盘启动盘"; } }
        public override bool CanGoNext { get { return _finished; } }

        public WriteProgressPage(InstallContext ctx)
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
                _ctx.Logger.Warn("用户取消了制作流程。");
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
            var usb = _ctx.SelectedUsb;
            if (item == null || usb == null)
            {
                ShowFail("资源或 U 盘未选择，无法开始制作。");
                return;
            }

            if (!UsbScanner.IsStillRemovable(usb.DriveLetter))
            {
                ShowFail("所选 U 盘已不在位（可能被拔出），请返回重新检测。");
                return;
            }

            if (!UsbWriter.HasEnoughSpace(usb, item.FileSize))
            {
                ShowFail("U 盘可用空间不足，无法写入该资源。");
                return;
            }

            try
            {
                // 1) 下载
                _progress.SetProgress(0, "开始下载资源…", "");
                _packageFile = System.IO.Path.Combine(AppPaths.DownloadDir,
                    Sanitize(item.Name) + "_" + (item.Version ?? "v") + ".pkg");
                var downloader = new Downloader(_ctx.Logger);
                var dlProgress = new Progress<DownloadProgressInfo>(p =>
                {
                    _progress.SetProgress(p.Percent,
                        p.StatusText ?? ("正在下载… " + p.FormatSize(p.BytesReceived)),
                        p.SpeedBytesPerSec > 0 ? FormatSpeed(p.SpeedBytesPerSec) : "");
                });

                var dl = await downloader.DownloadAsync(item, _packageFile, dlProgress, ct)
                    .ConfigureAwait(true);
                if (!dl.Success)
                {
                    ShowFail(dl.ErrorMessage);
                    return;
                }

                // 2) 清空 U 盘
                _progress.SetProgress(0, "正在清空 U 盘…", "");
                var writer = new UsbWriter(_ctx.Logger);
                var clearProgress = new Progress<int>(v =>
                    _progress.SetProgress(v, "正在清空 U 盘…", ""));
                await writer.ClearAsync(usb, clearProgress, ct).ConfigureAwait(true);

                // 3) 写入
                _progress.SetProgress(0, "正在写入资源到 U 盘…", "");
                var writeProgress = new Progress<int>(v =>
                    _progress.SetProgress(v, "正在写入资源到 U 盘…", ""));
                await writer.WriteAsync(_packageFile, usb, writeProgress, ct).ConfigureAwait(true);

                // 4) 标记
                writer.WriteMarker(usb, item.Name, item.Version);

                _progress.SetProgress(100, "制作完成，U 盘已可用。", "");
                _finished = true;
                if (Owner != null) { Owner.SetNextEnabled(true); Owner.UpdateNextButton(); }
                if (OnFinished != null) OnFinished();
            }
            catch (OperationCanceledException)
            {
                _progress.SetProgress(0, "已取消。", "");
                _ctx.Logger.Warn("制作流程已取消。");
                if (Owner != null) { Owner.SetNextEnabled(false); Owner.UpdateNextButton(); }
            }
            catch (Exception ex)
            {
                ShowFail("制作失败：" + ex.Message);
            }
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

        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec < 1024) return bytesPerSec.ToString("F0") + " B/s";
            if (bytesPerSec < 1024 * 1024) return (bytesPerSec / 1024).ToString("F1") + " KB/s";
            return (bytesPerSec / (1024 * 1024)).ToString("F2") + " MB/s";
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "resource";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
