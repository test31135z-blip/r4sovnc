using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    public partial class CameraForm : Form
    {
        private readonly string _clientLabel;

        public CameraForm(string clientLabel)
        {
            _clientLabel = clientLabel;
            InitializeComponent();
            this.Text = $"R4SoVNC — Camera: {clientLabel}";
        }

        public void UpdateFrame(byte[] jpegData)
        {
            if (InvokeRequired) { Invoke(() => UpdateFrame(jpegData)); return; }
            try
            {
                using var ms = new MemoryStream(jpegData);
                var img = Image.FromStream(ms);
                var old = pictureBox.Image;
                pictureBox.Image = img;
                old?.Dispose();
            }
            catch { }
        }

        public void SetStatus(string msg)
        {
            if (InvokeRequired) { Invoke(() => SetStatus(msg)); return; }
            lblStatus.Text = msg;
        }
    }
}
