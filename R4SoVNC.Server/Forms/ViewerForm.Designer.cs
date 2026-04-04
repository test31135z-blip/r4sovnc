using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class ViewerForm
    {
        private System.ComponentModel.IContainer components = null!;
        private PictureBox pictureBox;
        private Panel pnlToolbar;
        private Button btnToggleControl;
        private Button btnMic;
        private Button btnCamera;
        private Button btnShell;
        private Button btnFileManager;
        private Button btnFullscreen;
        private Label lblControlStatus;
        private Label lblResolution;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.StartPosition = FormStartPosition.CenterScreen;

            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.PanelDark
            };

            int bx = 10;

            btnToggleControl = MakeBtn("🟢 Start Control", ref bx, Theme.AccentDark, 140);
            btnToggleControl.Click += btnToggleControl_Click;

            btnMic = MakeBtn("🎤 Mic", ref bx, Theme.SurfaceLight, 90);
            btnMic.Click += btnMic_Click;

            btnCamera = MakeBtn("📷 Camera", ref bx, Theme.SurfaceLight, 100);
            btnCamera.Click += btnCamera_Click;

            btnShell = MakeBtn("⌨ Shell", ref bx, Theme.SurfaceLight, 90);
            btnShell.Click += btnShell_Click;

            btnFileManager = MakeBtn("📁 Files", ref bx, Theme.SurfaceLight, 90);
            btnFileManager.Click += btnFileManager_Click;

            // Separator
            bx += 6;
            var sep = new Label
            {
                Text = "|",
                ForeColor = Theme.Border,
                Font = new Font("Segoe UI", 14f),
                AutoSize = true,
                Location = new Point(bx, 15)
            };
            pnlToolbar.Controls.Add(sep);
            bx += 18;

            btnFullscreen = MakeBtn("⛶ Fullscreen", ref bx, Theme.SurfaceLight, 110);
            btnFullscreen.Click += btnFullscreen_Click;

            bx += 14;

            lblControlStatus = new Label
            {
                Text = "● VIEW ONLY",
                ForeColor = Theme.Success,
                Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(bx, 18)
            };

            lblResolution = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(bx + 135, 20)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                btnToggleControl, btnMic, btnCamera, btnShell, btnFileManager,
                btnFullscreen, lblControlStatus, lblResolution
            });
            this.Controls.Add(pnlToolbar);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox);
        }

        private Button MakeBtn(string text, ref int x, Color bg, int width = 110)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, 36),
                Location = new Point(x, 8),
                BackColor = bg
            };
            Theme.ApplyButton(btn, false);
            btn.BackColor = bg;
            x += width + 5;
            return btn;
        }
    }
}
