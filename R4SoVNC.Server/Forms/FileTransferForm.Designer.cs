using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class FileTransferForm
    {
        private System.ComponentModel.IContainer components = null!;
        private SplitContainer splitContainer;
        private ListView listLocal;
        private ListView listRemote;
        private Panel pnlHeader;
        private Label lblTitle;
        private Panel pnlLocalHeader;
        private Panel pnlRemoteHeader;
        private Label lblLocalPath;
        private Label lblRemotePath;
        private Panel pnlBottom;
        private ProgressBar progressTransfer;
        private Label lblTransferStatus;
        private Button btnUpload;
        private Button btnDownload;
        private Button btnRefresh;
        private Button btnNavUp;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "R4SoVNC — File Manager";
            this.Size = new Size(1000, 620);
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.StartPosition = FormStartPosition.CenterParent;

            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.PanelDark
            };
            lblTitle = new Label
            {
                Text = "📁  File Manager — Remote & Local",
                Font = new Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(16, 14)
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Bottom transfer bar
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.PanelDark,
                Padding = new Padding(15, 8, 15, 8)
            };

            btnUpload = new Button
            {
                Text = "⬆ Upload →",
                Size = new Size(110, 32),
                Location = new Point(15, 14)
            };
            Theme.ApplyButton(btnUpload, true);
            btnUpload.Click += btnUpload_Click;

            btnDownload = new Button
            {
                Text = "⬇ Download ←",
                Size = new Size(120, 32),
                Location = new Point(135, 14)
            };
            Theme.ApplyButton(btnDownload, false);
            btnDownload.Click += btnDownload_Click;

            btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Size = new Size(90, 32),
                Location = new Point(265, 14)
            };
            Theme.ApplyButton(btnRefresh, false);
            btnRefresh.Click += btnRefresh_Click;

            btnNavUp = new Button
            {
                Text = "↑ Up",
                Size = new Size(75, 32),
                Location = new Point(365, 14)
            };
            Theme.ApplyButton(btnNavUp, false);
            btnNavUp.Click += btnNavUp_Click;

            progressTransfer = new ProgressBar
            {
                Size = new Size(300, 14),
                Location = new Point(460, 10),
                Style = ProgressBarStyle.Continuous
            };

            lblTransferStatus = new Label
            {
                Text = "Ready",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(460, 30)
            };

            pnlBottom.Controls.AddRange(new Control[] {
                btnUpload, btnDownload, btnRefresh, btnNavUp, progressTransfer, lblTransferStatus
            });
            this.Controls.Add(pnlBottom);

            // Split container
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Border,
                SplitterWidth = 4,
                SplitterDistance = 490
            };

            // Local panel
            pnlLocalHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Theme.SurfaceLight
            };
            var lblLocalTitle = new Label
            {
                Text = "  LOCAL",
                Font = new Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 7)
            };
            lblLocalPath = new Label
            {
                Text = "",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(70, 8)
            };
            pnlLocalHeader.Controls.AddRange(new Control[] { lblLocalTitle, lblLocalPath });

            listLocal = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                View = View.Details,
                AllowDrop = false
            };
            listLocal.Columns.Add("Name", 260);
            listLocal.Columns.Add("Size", 100);
            listLocal.DoubleClick += listLocal_DoubleClick;

            splitContainer.Panel1.Controls.Add(listLocal);
            splitContainer.Panel1.Controls.Add(pnlLocalHeader);

            // Remote panel
            pnlRemoteHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Theme.SurfaceLight
            };
            var lblRemoteTitle = new Label
            {
                Text = "  REMOTE",
                Font = new Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(4, 7)
            };
            lblRemotePath = new Label
            {
                Text = @"C:\",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(74, 8)
            };
            pnlRemoteHeader.Controls.AddRange(new Control[] { lblRemoteTitle, lblRemotePath });

            listRemote = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                View = View.Details,
                AllowDrop = true
            };
            listRemote.Columns.Add("Name", 220);
            listRemote.Columns.Add("Size", 90);
            listRemote.Columns.Add("Modified", 130);
            listRemote.DoubleClick += listRemote_DoubleClick;

            splitContainer.Panel2.Controls.Add(listRemote);
            splitContainer.Panel2.Controls.Add(pnlRemoteHeader);

            this.Controls.Add(splitContainer);
        }
    }
}
