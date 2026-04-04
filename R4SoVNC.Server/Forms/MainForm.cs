using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;
using R4SoVNC.Server.Network;
using R4SoVNC.Server.Protocol;

namespace R4SoVNC.Server.Forms
{
    public partial class MainForm : Form
    {
        private readonly R4VNCServer _server = new();
        private readonly List<ClientSession> _sessions = new();
        private readonly System.Windows.Forms.Timer _statusTimer = new();
        private DateTime _startTime = DateTime.Now;

        public MainForm()
        {
            InitializeComponent();
            SetupServer();
            SetupTimer();
        }

        private void SetupServer()
        {
            _server.ClientConnected    += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.LogMessage         += msg => AppendLog(msg);
        }

        private void SetupTimer()
        {
            _statusTimer.Interval = 1000;
            _statusTimer.Tick += (s, e) => UpdateStatusBar();
            _statusTimer.Start();
        }

        private void OnClientConnected(ClientSession session)
        {
            this.Invoke(() =>
            {
                _sessions.Add(session);
                RefreshClientList();
                AppendLog($"[+] {session.IpAddress}  (ID: {session.SessionId})");
            });
        }

        private void OnClientDisconnected(ClientSession session)
        {
            this.Invoke(() =>
            {
                _sessions.Remove(session);
                RefreshClientList();
                AppendLog($"[-] {session.IpAddress} disconnected");
            });
        }

        private void RefreshClientList()
        {
            listClients.Items.Clear();
            var lblEmpty = pnlMain.Controls["lblEmpty"];

            foreach (var s in _sessions)
            {
                var item = new ListViewItem(s.SessionId);
                item.SubItems.Add(s.ClientName);
                item.SubItems.Add(s.IpAddress);
                item.SubItems.Add(s.ConnectedAt.ToString("HH:mm:ss"));
                item.SubItems.Add("● Online");
                item.Tag = s;
                item.ForeColor = Theme.Text;
                item.UseItemStyleForSubItems = false;
                item.SubItems[4].ForeColor = Theme.Success;
                listClients.Items.Add(item);
            }

            lblClientsCount.Text = $"{_sessions.Count} online";
            lblClientsCount.ForeColor = _sessions.Count > 0 ? Theme.Success : Theme.TextMuted;
            if (lblEmpty != null) lblEmpty.Visible = _sessions.Count == 0;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired) { Invoke(() => AppendLog(message)); return; }
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = Theme.TextMuted;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            rtbLog.ScrollToCaret();
        }

        private void UpdateStatusBar()
        {
            if (_server.IsRunning)
                lblUptime.Text = $"Uptime: {(DateTime.Now - _startTime):hh\\:mm\\:ss}";
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();
                btnStartServer.Text = "▶  Start Listening";
                Theme.ApplyButton(btnStartServer, true);
                lblServerStatus.Text = "Server Offline";
                lblServerStatus.ForeColor = Theme.Danger;
                lblStatusDot.ForeColor = Theme.Danger;
                lblStatus.Text = "Stopped";
                txtPort.Enabled = true;
            }
            else
            {
                if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid port (1–65535).", "Invalid Port",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    _server.Start(port);
                    _startTime = DateTime.Now;
                    btnStartServer.Text = "■  Stop Listening";
                    Theme.ApplyButton(btnStartServer, false);
                    btnStartServer.BackColor = Theme.Danger;
                    lblServerStatus.Text = $"Listening on :{port}";
                    lblServerStatus.ForeColor = Theme.Success;
                    lblStatusDot.ForeColor = Theme.Success;
                    lblStatus.Text = $"Listening on port {port}";
                    txtPort.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (listClients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a client from the list first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var session = (ClientSession)listClients.SelectedItems[0].Tag!;
            OpenViewer(session);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (listClients.SelectedItems.Count == 0) return;
            var session = (ClientSession)listClients.SelectedItems[0].Tag!;
            if (MessageBox.Show($"Disconnect {session.IpAddress}?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                session.Disconnect();
        }

        private void btnBuilder_Click(object sender, EventArgs e)
        {
            new BuilderForm().ShowDialog(this);
        }

        private void listClients_DoubleClick(object sender, EventArgs e)
        {
            if (listClients.SelectedItems.Count == 0) return;
            OpenViewer((ClientSession)listClients.SelectedItems[0].Tag!);
        }

        private void OpenViewer(ClientSession session)
        {
            var viewer = new ViewerForm(session);
            viewer.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_server.IsRunning)
            {
                var r = MessageBox.Show("Server is running. Stop and exit?", "Confirm Exit",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.No) { e.Cancel = true; return; }
            }
            _server.Stop();
            base.OnFormClosing(e);
        }
    }
}
