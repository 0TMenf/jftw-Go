using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JftwGo.Core.Usb;
using JftwGo.UI;
using JftwGo.UI.Wizard;

using JftwGo.Core.Localization;

namespace JftwGo.Installer.Pages
{
    /// <summary>U 盘选择与检测页：显示容量、文件系统、可用空间。</summary>
    public class UsbSelectPage : WizardPage
    {
        private readonly InstallContext _ctx;
        private readonly ListBox _list;
        private readonly Label _lblDetail;
        private readonly Label _lblHint;
        private List<UsbDeviceInfo> _devices = new List<UsbDeviceInfo>();

        public override string PageTitle { get { return L.Get("usb.title"); } }

        public override bool CanGoNext
        {
            get { return _ctx.SelectedUsb != null; }
        }

        public UsbSelectPage(InstallContext ctx)
        {
            _ctx = ctx;
            BackColor = UiTheme.Page;

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(20) };

            var title = new Label
            {
                Text = L.Get("usb.detected"),
                Font = UiTheme.FontSubTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 38 };
            var btnRefresh = UiTheme.MakeButton(L.Get("usb.rescan"), false);
            btnRefresh.Location = new Point(0, 4);
            btnRefresh.Click += (s, e) => Scan();
            toolbar.Controls.Add(btnRefresh);

            _list = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 132,
                Font = UiTheme.FontNormal,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            _list.SelectedIndexChanged += (s, e) => OnSelectionChanged();

            _lblDetail = new Label
            {
                Text = L.Get("usb.notSelected"),
                Font = UiTheme.FontNormal,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 96,
                Padding = new Padding(0, 12, 0, 0)
            };

            _lblHint = new Label
            {
                Text = L.Get("usb.hint"),
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.Danger,
                Dock = DockStyle.Bottom,
                Height = 44
            };

            card.Controls.Add(_lblDetail);
            card.Controls.Add(_list);
            card.Controls.Add(toolbar);
            card.Controls.Add(title);
            card.Controls.Add(_lblHint);
            Controls.Add(card);
        }

        public override void OnEnter()
        {
            Scan();
            if (Owner != null) Owner.UpdateNextButton();
        }

        private void Scan()
        {
            _devices = UsbScanner.Scan();
            _list.Items.Clear();
            foreach (var d in _devices)
                _list.Items.Add(d.DisplayName);

            _ctx.SelectedUsb = null;
            _lblDetail.Text = _devices.Count == 0
                ? L.Get("usb.notDetected")
                : L.Get("usb.pleaseSelect");
            _lblDetail.ForeColor = UiTheme.TextSecondary;
            if (Owner != null) Owner.UpdateNextButton();
        }

        private void OnSelectionChanged()
        {
            int i = _list.SelectedIndex;
            if (i < 0 || i >= _devices.Count) return;
            var d = _devices[i];
            _ctx.SelectedUsb = d;
            _lblDetail.Text = string.Format(
                L.Get("usb.detail"),
                d.DriveLetter,
                string.IsNullOrWhiteSpace(d.VolumeLabel) ? L.Get("common.none") : d.VolumeLabel,
                string.IsNullOrWhiteSpace(d.FileSystem) ? L.Get("common.unknown") : d.FileSystem,
                d.CapacityText,
                d.FreeText);
            _lblDetail.ForeColor = UiTheme.TextPrimary;
            if (Owner != null) Owner.UpdateNextButton();
        }
    }
}
