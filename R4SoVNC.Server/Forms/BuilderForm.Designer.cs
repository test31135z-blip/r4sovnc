using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class BuilderForm
    {
        private System.ComponentModel.IContainer components = null!;

        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblDesc;

        private Label lblHostLabel;
        private TextBox txtHost;
        private Label lblPortLabel;
        private TextBox txtPort;
        private Label lblOutputLabel;
        private TextBox txtOutputName;
        private Button btnBrowseOutput;
        private CheckBox chkOpenFolder;
        private Button btnBuild;
        private Panel pnlProgress;
        private ProgressBar progressBar;
        private Label lblBuildStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "R4SoVNC — Client Builder";
            this.Size = new Size(520, 440);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.StartPosition = FormStartPosition.CenterParent;

            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.PanelDark
            };

            lblTitle = new Label
            {
                Text = "⚙  Client Builder",
                Font = Theme.FontTitle,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(20, 12)
            };

            lblDesc = new Label
            {
                Text = "Configure and build a self-contained client executable",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(24, 45)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblDesc });
            this.Controls.Add(pnlHeader);

            // Form Fields
            int x = 30, startY = 90, spacing = 60;

            lblHostLabel = MakeLabel("Server Host / IP Address", x, startY);
            txtHost = MakeTextBox(x, startY + 22, 440, "e.g. 192.168.1.100 or yourdomain.com");

            lblPortLabel = MakeLabel("Server Port", x, startY + spacing);
            txtPort = MakeTextBox(x, startY + spacing + 22, 200, "7890");
            txtPort.Text = "7890";

            lblOutputLabel = MakeLabel("Output Filename", x, startY + spacing * 2);
            txtOutputName = MakeTextBox(x, startY + spacing * 2 + 22, 360, "r4client.exe");
            txtOutputName.Text = "r4client.exe";

            btnBrowseOutput = new Button
            {
                Text = "...",
                Location = new Point(x + 366, startY + spacing * 2 + 22),
                Size = new Size(74, 28)
            };
            Theme.ApplyButton(btnBrowseOutput, false);
            btnBrowseOutput.Click += btnBrowseOutput_Click;

            chkOpenFolder = new CheckBox
            {
                Text = "Open output folder after build",
                Location = new Point(x, startY + spacing * 3 + 5),
                ForeColor = Theme.TextMuted,
                Font = Theme.FontBody,
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };

            btnBuild = new Button
            {
                Text = "⚙  Build Client",
                Location = new Point(x, startY + spacing * 3 + 40),
                Size = new Size(440, 44)
            };
            Theme.ApplyButton(btnBuild, true);
            btnBuild.Font = new Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            btnBuild.Click += btnBuild_Click;

            // Progress
            pnlProgress = new Panel
            {
                Location = new Point(x, startY + spacing * 3 + 94),
                Size = new Size(440, 50),
                BackColor = Theme.Background,
                Visible = false
            };

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(440, 16),
                Location = new Point(0, 0)
            };

            lblBuildStatus = new Label
            {
                Text = "Building...",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(0, 22)
            };

            pnlProgress.Controls.AddRange(new Control[] { progressBar, lblBuildStatus });

            this.Controls.AddRange(new Control[]
            {
                lblHostLabel, txtHost,
                lblPortLabel, txtPort,
                lblOutputLabel, txtOutputName, btnBrowseOutput,
                chkOpenFolder, btnBuild, pnlProgress
            });
        }

        private Label MakeLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(x, y)
            };
            return lbl;
        }

        private TextBox MakeTextBox(int x, int y, int width, string placeholder)
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 28
            };
            Theme.ApplyTextBox(tb);
            return tb;
        }
    }
}
