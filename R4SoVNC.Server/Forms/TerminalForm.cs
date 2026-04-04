using System;
using System.Drawing;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;
using R4SoVNC.Server.Network;
using R4SoVNC.Server.Protocol;

namespace R4SoVNC.Server.Forms
{
    public partial class TerminalForm : Form
    {
        private readonly ClientSession _session;

        public TerminalForm(ClientSession session)
        {
            _session = session;
            InitializeComponent();
            Text = $"R4SoVNC — Shell: {session.ClientName} [{session.IpAddress}]";

            session.PacketReceived += OnPacketReceived;
            session.Disconnected   += OnDisconnected;

            AppendOutput($"R4SoVNC Remote Shell — Connected to {session.ClientName} [{session.IpAddress}]");
            AppendOutput("Type commands below and press Enter or click Send.");
            AppendOutput(new string('─', 60));
            AppendOutput("");

            _session.SendPacket(PacketBuilder.ShellStart());
        }

        private void OnPacketReceived(ClientSession session, Packet packet)
        {
            if (packet.Type == PacketType.ShellOutput)
                AppendOutput(packet.GetDataAsString());
        }

        private void OnDisconnected(ClientSession session)
        {
            this.Invoke(() =>
            {
                AppendOutput("\n[!] Client disconnected.");
                txtInput.Enabled = false;
                btnSend.Enabled = false;
            });
        }

        public void AppendOutput(string text)
        {
            if (InvokeRequired) { Invoke(() => AppendOutput(text)); return; }

            rtbOutput.SelectionStart = rtbOutput.TextLength;
            rtbOutput.SelectionLength = 0;

            // Color output lines differently
            if (text.StartsWith("R4SoVNC") || text.StartsWith("Type") || text.All(c => c == '─' || c == ' '))
            {
                rtbOutput.SelectionColor = Theme.TextMuted;
            }
            else if (text.StartsWith("[!]") || text.StartsWith("Error") || text.Contains("not recognized"))
            {
                rtbOutput.SelectionColor = Theme.Danger;
            }
            else if (text.StartsWith(">"))
            {
                rtbOutput.SelectionColor = Theme.Accent;
            }
            else
            {
                rtbOutput.SelectionColor = Theme.Text;
            }

            rtbOutput.AppendText(text + "\n");
            rtbOutput.ScrollToCaret();
        }

        private void SendCommand()
        {
            string cmd = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            AppendOutput($"> {cmd}");
            _session.SendPacket(PacketBuilder.ShellCommand(cmd));
            txtInput.Clear();
        }

        private void btnSend_Click(object sender, EventArgs e) => SendCommand();

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendCommand();
            }
            else if (e.KeyCode == Keys.Up)
            {
                // Command history - future enhancement
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
            AppendOutput("Terminal cleared.");
            AppendOutput("");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _session.PacketReceived -= OnPacketReceived;
            _session.Disconnected -= OnDisconnected;
            try { _session.SendPacket(PacketBuilder.ShellStop()); } catch { }
            base.OnFormClosing(e);
        }
    }
}
