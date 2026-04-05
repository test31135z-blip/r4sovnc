using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace R4SoVNC.ClientEmbed.Capture
{
    internal class ScreenCapturer
    {
        private readonly int _quality;

        public ScreenCapturer(int jpegQuality = 50) => _quality = jpegQuality;

        public byte[] Capture()
        {
            var bounds = SystemInformation.VirtualScreen;
            using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using var g   = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            using var ms   = new MemoryStream();
            var       enc  = GetJpegEncoder();
            var       pars = new EncoderParameters(1);
            pars.Param[0]  = new EncoderParameter(Encoder.Quality, (long)_quality);
            bmp.Save(ms, enc, pars);
            return ms.ToArray();
        }

        private static ImageCodecInfo GetJpegEncoder()
        {
            foreach (var c in ImageCodecInfo.GetImageEncoders())
                if (c.MimeType == "image/jpeg") return c;
            throw new Exception("JPEG encoder not found");
        }
    }
}
