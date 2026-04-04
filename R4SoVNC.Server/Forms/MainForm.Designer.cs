using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;

        // Header
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;

        // Left sidebar
        private Panel pnlSidebar;

        // Port + Listen section
        private Panel pnlListenBox;
        private Label lblListenTitle;
        private Label lblPortLabel;
        private TextBox txtPort;
        private Button btnStartServer;
        private Label lblServerStatus;
        private Label lblStatusDot;

        // Builder
        private Button btnBuilder;

        // Client actions
        private Panel pnlActionsBox;
        private Label lblActionsTitle;
        private Button btnConnect;
        private Button btnDisconnect;

        // Log
        private Panel pnlLogBox;
        private Label lblLogTitle;
        private RichTextBox rtbLog;

        // Main area
        private Panel pnlMain;
        private Panel pnlClientsHeader;
        private Label lblClientsTitle;
        private Label lblClientsCount;
        private ListView listClients;

        // Status bar
        private Panel pnlStatusBar;
        private Label lblStatus;
        private Label lblUptime;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "R4SoVNC — Remote Access Server";
            this.Size = new Size(1120, 740);
            this.MinimumSize = new Size(900, 600);
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.Text;
            this.Font = Theme.FontBody;
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Header ───────────────────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.PanelDark
            };

            lblTitle = new Label
            {
                Text = "R4SoVNC",
                Font = Theme.FontTitle,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(22, 10)
            };

            lblSubtitle = new Label
            {
                Text = "Remote Access & Control Platform",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(26, 44)
            };

            lblStatusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 13f),
                ForeColor = Theme.Danger,
                AutoSize = true,
                Location = new Point(240, 18)
            };

            lblServerStatus = new Label
            {
                Text = "Server Offline",
                Font = Theme.FontBold,
                ForeColor = Theme.Danger,
                AutoSize = true,
                Location = new Point(265, 24)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblStatusDot, lblServerStatus });
            this.Controls.Add(pnlHeader);

            // ── Sidebar ──────────────────────────────────────────────────────
            pnlSidebar = new Panel
            {
                Width = 270,
                Dock = DockStyle.Left,
                BackColor = Theme.Surface
            };

            // Port / Listen box
            pnlListenBox = new Panel
            {
                Location = new Point(0, 0),
                Width = 270,
                Height = 195,
                BackColor = Theme.SurfaceLight,
                Padding = new Padding(15)
            };

            lblListenTitle = new Label
            {
                Text = "▶  PORT LISTENING",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(15, 14)
            };

            lblPortLabel = new Label
            {
                Text = "Listen Port",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(15, 38)
            };

            txtPort = new TextBox
            {
                Text = "7890",
                Location = new Point(15, 55),
                Width = 238,
                Height = 30,
                Font = new Font("Consolas", 14f, FontStyle.Bold)
            };
            Theme.ApplyTextBox(txtPort);
            txtPort.ForeColor = Theme.Accent;

            btnStartServer = new Button
            {
                Text = "▶  Start Listening",
                Location = new Point(15, 97),
                Width = 238,
                Height = 42
            };
            Theme.ApplyButton(btnStartServer, true);
            btnStartServer.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStartServer.Click += btnStartServer_Click;

            btnBuilder = new Button
            {
                Text = "⚙  Client Builder",
                Location = new Point(15, 149),
                Width = 238,
                Height = 34
            };
            Theme.ApplyButton(btnBuilder, false);
            btnBuilder.Click += btnBuilder_Click;

            pnlListenBox.Controls.AddRange(new Control[] {
                lblListenTitle, lblPortLabel, txtPort, btnStartServer, btnBuilder
            });

            // Client actions box
            pnlActionsBox = new Panel
            {
                Location = new Point(0, 200),
                Width = 270,
                Height = 120,
                BackColor = Theme.Surface,
                Padding = new Padding(15)
            };

            lblActionsTitle = new Label
            {
                Text = "CLIENT ACTIONS",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(15, 12)
            };

            btnConnect = new Button
            {
                Text = "🖥  Connect / View",
                Location = new Point(15, 32),
                Width = 238,
                Height = 36
            };
            Theme.ApplyButton(btnConnect, true);
            btnConnect.Click += btnConnect_Click;

            btnDisconnect = new Button
            {
                Text = "✕  Disconnect Client",
                Location = new Point(15, 76),
                Width = 238,
                Height = 36
            };
            Theme.ApplyButton(btnDisconnect, false);
            btnDisconnect.FlatAppearance.BorderColor = Theme.Danger;
            btnDisconnect.Click += btnDisconnect_Click;

            pnlActionsBox.Controls.AddRange(new Control[] {
                lblActionsTitle, btnConnect, btnDisconnect
            });

            // Log box
            pnlLogBox = new Panel
            {
                Location = new Point(0, 325),
                Width = 270,
                BackColor = Theme.Surface,
                Padding = new Padding(15)
            };
            pnlLogBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            lblLogTitle = new Label
            {
                Text = "EVENT LOG",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(15, 10)
            };

            rtbLog = new RichTextBox
            {
                Location = new Point(15, 30),
                Width = 238,
                BackColor = Theme.PanelDark,
                ForeColor = Theme.TextMuted,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontMono,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            pnlLogBox.Controls.AddRange(new Control[] { lblLogTitle, rtbLog });

            pnlSidebar.Controls.AddRange(new Control[] { pnlListenBox, pnlActionsBox, pnlLogBox });
            this.Controls.Add(pnlSidebar);

            // ── Main Panel ───────────────────────────────────────────────────
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(20)
            };

            pnlClientsHeader = new Panel
            {
                Location = new Point(20, 12),
                Height = 36,
                BackColor = Color.Transparent
            };
            pnlClientsHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblClientsTitle = new Label
            {
                Text = "Connected Clients",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(0, 4)
            };

            lblClientsCount = new Label
            {
                Text = "0 online",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(190, 10)
            };

            pnlClientsHeader.Controls.AddRange(new Control[] { lblClientsTitle, lblClientsCount });

            listClients = new ListView
            {
                Location = new Point(20, 55),
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                GridLines = false,
                View = View.Details,
                Font = Theme.FontBody,
                HoverSelection = true
            };
            listClients.Columns.Add("Session ID", 90);
            listClients.Columns.Add("Machine Name", 170);
            listClients.Columns.Add("IP Address", 165);
            listClients.Columns.Add("Connected At", 115);
            listClients.Columns.Add("Status", 100);
            listClients.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            listClients.DoubleClick += listClients_DoubleClick;

            // Empty state label
            var lblEmpty = new Label
            {
                Text = "No clients connected.\n\nStart the server and run a built client on the target machine.",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextMuted,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                AutoSize = false,
                Location = new Point(20, 200),
                Size = new Size(600, 80)
            };
            lblEmpty.Name = "lblEmpty";

            pnlMain.Controls.AddRange(new Control[] { pnlClientsHeader, listClients, lblEmpty });
            this.Controls.Add(pnlMain);

            // ── Status Bar ───────────────────────────────────────────────────
            pnlStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Theme.PanelDark
            };

            lblStatus = new Label
            {
                Text = "Ready",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(10, 8)
            };

            lblUptime = new Label
            {
                Text = "Uptime: 00:00:00",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                AutoSize = true,
                Location = new Point(220, 8)
            };

            pnlStatusBar.Controls.AddRange(new Control[] { lblStatus, lblUptime });
            this.Controls.Add(pnlStatusBar);

            // Resize handler
            this.Resize += (s, e) =>
            {
                pnlLogBox.Height = pnlSidebar.Height - 330;
                rtbLog.Height = pnlLogBox.Height - 50;
                pnlClientsHeader.Width = pnlMain.Width - 40;
                listClients.Width = pnlMain.Width - 40;
                listClients.Height = pnlMain.Height - 70;
            };
        }
    }
}
