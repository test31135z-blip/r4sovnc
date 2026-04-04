using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class CameraForm
    {
        private System.ComponentModel.IContainer components = null!;
        private PictureBox pictureBox;
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(420, 360);
            this.MinimumSize = new Size(320, 280);
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.TopMost = true;

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Theme.PanelDark
            };

            lblTitle = new Label
            {
                Text = "📷  Camera Feed",
                Font = new Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            lblStatus = new Label
            {
                Text = "Connecting...",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(140, 13)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblStatus });
            this.Controls.Add(pnlHeader);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox);
        }
    }
}
