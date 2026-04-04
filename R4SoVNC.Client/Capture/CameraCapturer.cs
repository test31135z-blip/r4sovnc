using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Video;
using AForge.Video.DirectShow;
using R4SoVNC.Client.Network;
using R4SoVNC.Client.Protocol;

namespace R4SoVNC.Client.Capture
{
    public class CameraCapturer : IDisposable
    {
        private VideoCaptureDevice? _device;
        private readonly ServerConnection _conn;
        private bool _active;

        private static readonly ImageCodecInfo _jpegCodec = GetJpegCodec();

        public CameraCapturer(ServerConnection conn)
        {
            _conn = conn;
        }

        public bool Start()
        {
            if (_active) return false;
            try
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (devices.Count == 0)
                {
                    Console.WriteLine("[R4SoVNC] No camera found.");
                    return false;
                }
                _device = new VideoCaptureDevice(devices[0].MonikerString);
                _device.NewFrame += OnNewFrame;
                _device.Start();
                _active = true;
                Console.WriteLine("[R4SoVNC] Camera started.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[R4SoVNC] Camera error: {ex.Message}");
                return false;
            }
        }

        private void OnNewFrame(object sender, NewFrameEventArgs e)
        {
            if (!_active) return;
            try
            {
                using var bmp = (Bitmap)e.Frame.Clone();
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, 60L);
                using var ms = new MemoryStream();
                bmp.Save(ms, _jpegCodec, ep);
                byte[] data = ms.ToArray();
                _conn.Send(new Packet(PacketType.CameraFrame, data));
            }
            catch { }
        }

        public void Stop()
        {
            _active = false;
            try
            {
                _device?.SignalToStop();
                _device?.WaitForStop();
            }
            catch { }
            _device = null;
        }

        public void Dispose() => Stop();

        private static ImageCodecInfo GetJpegCodec()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.FormatID == ImageFormat.Jpeg.Guid) return codec;
            return null!;
        }
    }
}
