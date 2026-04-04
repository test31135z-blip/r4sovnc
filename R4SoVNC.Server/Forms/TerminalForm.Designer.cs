using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class TerminalForm
    {
        private System.ComponentModel.IContainer components = null!;
        private Panel pnlHeader;
        private Label lblTitle;
        private RichTextBox rtbOutput;
        private Panel pnlInput;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnClear;
        private Label lblPrompt;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(850, 560);
            this.MinimumSize = new Size(600, 400);
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = Theme.FontMono;

            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.PanelDark
            };

            lblTitle = new Label
            {
                Text = "⌨  Remote Shell (CMD)",
                Font = new Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            var lblNote = new Label
            {
                Text = "Commands run on the remote machine",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(240, 16)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblNote });
            this.Controls.Add(pnlHeader);

            // Output
            rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(5, 8, 18),
                ForeColor = Theme.Text,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(10)
            };
            this.Controls.Add(rtbOutput);

            // Input bar
            pnlInput = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Theme.PanelDark,
                Padding = new Padding(8, 6, 8, 6)
            };

            lblPrompt = new Label
            {
                Text = "C:\\> ",
                Font = new Font("Consolas", 11f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(10, 13)
            };

            txtInput = new TextBox
            {
                Location = new Point(56, 10),
                Height = 28,
                BackColor = Color.FromArgb(10, 15, 30),
                ForeColor = Color.FromArgb(200, 230, 255),
                Font = new Font("Consolas", 11f),
                BorderStyle = BorderStyle.None
            };
            txtInput.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            txtInput.KeyDown += txtInput_KeyDown;

            btnSend = new Button
            {
                Text = "Send ↵",
                Size = new Size(80, 28),
                BackColor = Theme.Accent
            };
            btnSend.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            Theme.ApplyButton(btnSend, true);
            btnSend.Click += btnSend_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Size = new Size(60, 28),
                BackColor = Theme.SurfaceLight
            };
            btnClear.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            Theme.ApplyButton(btnClear, false);
            btnClear.Click += btnClear_Click;

            pnlInput.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnSend, btnClear });
            this.Controls.Add(pnlInput);

            // Resize handler for input bar
            this.Resize += (s, e) =>
            {
                txtInput.Width = pnlInput.Width - 220;
                btnSend.Location = new Point(pnlInput.Width - 155, 9);
                btnClear.Location = new Point(pnlInput.Width - 70, 9);
            };
        }
    }
}
