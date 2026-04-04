using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace R4SoVNC.Client.Capture
{
    public class ScreenCapturer
    {
        private readonly int _quality;
        private static readonly ImageCodecInfo JpegCodec;
        private static readonly EncoderParameters EncoderParams;

        static ScreenCapturer()
        {
            JpegCodec = GetJpegCodec();
        }

        public ScreenCapturer(int jpegQuality = 50)
        {
            _quality = jpegQuality;
            EncoderParameters ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
        }

        public byte[] Capture()
        {
            var screen = Screen.PrimaryScreen!.Bounds;
            using var bmp = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(screen.Location, Point.Empty, screen.Size);

            // Draw cursor
            DrawCursor(g);

            var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(Encoder.Quality, (long)_quality);

            using var ms = new MemoryStream();
            bmp.Save(ms, JpegCodec, ep);
            return ms.ToArray();
        }

        private static void DrawCursor(Graphics g)
        {
            try
            {
                var ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(ci);
                if (GetCursorInfo(out ci) && ci.flags == 1)
                {
                    var cursor = new Cursor(ci.hCursor);
                    var pt = new Point(ci.ptScreenPos.x - cursor.HotSpot.X,
                                      ci.ptScreenPos.y - cursor.HotSpot.Y);
                    cursor.Draw(g, new Rectangle(pt, cursor.Size));
                }
            }
            catch { }
        }

        private static ImageCodecInfo GetJpegCodec()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.FormatID == ImageFormat.Jpeg.Guid) return codec;
            return null!;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }
    }
}
