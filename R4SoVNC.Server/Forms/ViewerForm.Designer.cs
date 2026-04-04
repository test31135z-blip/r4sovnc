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
        private Button btnFileManager;
        private Button btnFullscreen;
        private Button btnMic;
        private Button btnCamera;
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

            // Toolbar
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.PanelDark
            };

            int bx = 10;

            btnToggleControl = MakeToolBtn("🟢 Start Control", bx, Theme.AccentDark);
            btnToggleControl.Click += btnToggleControl_Click;
            bx += btnToggleControl.Width + 6;

            btnMic = MakeToolBtn("🎤 Mic", bx, Theme.SurfaceLight);
            btnMic.Click += btnMic_Click;
            bx += btnMic.Width + 6;

            btnCamera = MakeToolBtn("📷 Camera", bx, Theme.SurfaceLight);
            btnCamera.Click += btnCamera_Click;
            bx += btnCamera.Width + 6;

            btnFileManager = MakeToolBtn("📁 Files", bx, Theme.SurfaceLight);
            btnFileManager.Click += btnFileManager_Click;
            bx += btnFileManager.Width + 6;

            btnFullscreen = MakeToolBtn("⛶ Fullscreen", bx, Theme.SurfaceLight);
            btnFullscreen.Click += btnFullscreen_Click;
            bx += btnFullscreen.Width + 16;

            lblControlStatus = new Label
            {
                Text = "● VIEW ONLY",
                ForeColor = Theme.Success,
                Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(bx, 17)
            };

            lblResolution = new Label
            {
                Text = "?×?",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(bx + 140, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                btnToggleControl, btnMic, btnCamera, btnFileManager, btnFullscreen,
                lblControlStatus, lblResolution
            });
            this.Controls.Add(pnlToolbar);

            // Screen view
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox);
        }

        private Button MakeToolBtn(string text, int x, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(120, 34),
                Location = new Point(x, 8),
                BackColor = bg
            };
            Theme.ApplyButton(btn, false);
            btn.BackColor = bg;
            return btn;
        }
    }
}
