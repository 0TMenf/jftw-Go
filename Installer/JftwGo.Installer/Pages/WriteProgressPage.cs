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

using JftwGo.Core.Localization;

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

        public override string PageTitle { get { return L.Get("write.title"); } }
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
            _btnRetry = UiTheme.MakeButton(L.Get("common.retry"), false);
            _btnRetry.Location = new Point(0, 4);
            _btnRetry.Enabled = false;
            _btnRetry.Click += (s, e) => { _ = RunAsync(); };
            var btnCancel = UiTheme.MakeButton(L.Get("common.cancel"), false);
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
                _ctx.Logger.Warn(L.Get("write.userCancel"));
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
                ShowFail(L.Get("write.noSelect"));
                return;
            }

            if (!UsbScanner.IsStillRemovable(usb.DriveLetter))
            {
                ShowFail(L.Get("write.usbGone"));
                return;
            }

            if (!UsbWriter.HasEnoughSpace(usb, item.FileSize))
            {
                ShowFail(L.Get("write.noSpace"));
                return;
            }

            try
            {
                // 1) 下载
                _progress.SetProgress(0, L.Get("write.downloading"), "");
                _packageFile = System.IO.Path.Combine(AppPaths.DownloadDir,
                    Sanitize(item.Name) + "_" + (item.Version ?? "v") + ".pkg");
                var downloader = new Downloader(_ctx.Logger);
                var dlProgress = new Progress<DownloadProgressInfo>(p =>
                {
                    _progress.SetProgress(p.Percent,
                        p.StatusText ?? L.Format("write.downloadingFmt", p.FormatSize(p.BytesReceived)),
                        p.SpeedBytesPerSec > 0 ? FormatSpeed(p.SpeedBytesPerSec) : "");
                });

                var dl = await downloader.DownloadAsync(item, _packageFile, dlProgress, ct)
                    .ConfigureAwait(true);
                if (!dl.Success)
                {
                    ShowFail(dl.ErrorMessage);
                    return;
                }

                // 2) 部署资源整合包（不格式化、不清盘，按整合包分目录）
                _progress.SetProgress(0, L.Get("write.deploying"), "");
                var writer = new UsbWriter(_ctx.Logger);
                var writeProgress = new Progress<int>(v =>
                    _progress.SetProgress(v, L.Get("write.deploying"), ""));
                var deploy = await writer.DeployAsync(_packageFile, usb, item, writeProgress, ct)
                    .ConfigureAwait(true);
                if (!deploy.Success)
                {
                    ShowFail(L.Format("write.deployFail", deploy.Warning ?? L.Get("write.unknown")));
                    return;
                }
                _ctx.DeployResult = deploy;

                string tail = deploy.LauncherFound
                    ? L.Get("write.shortcutOk")
                    : (deploy.Warning ?? "");
                _progress.SetProgress(100, L.Format("write.done", tail), "");
                _finished = true;
                if (Owner != null) { Owner.SetNextEnabled(true); Owner.UpdateNextButton(); }
                if (OnFinished != null) OnFinished();
            }
            catch (OperationCanceledException)
            {
                _progress.SetProgress(0, L.Get("write.canceled"), "");
                _ctx.Logger.Warn(L.Get("write.userCancel"));
                if (Owner != null) { Owner.SetNextEnabled(false); Owner.UpdateNextButton(); }
            }
            catch (Exception ex)
            {
                ShowFail(L.Format("write.fail", ex.Message));
            }
        }

        private void ShowFail(string message)
        {
            _ctx.Logger.Error(message);
            _progress.SetProgress(0, L.Format("write.failedPrefix", message), "");
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
