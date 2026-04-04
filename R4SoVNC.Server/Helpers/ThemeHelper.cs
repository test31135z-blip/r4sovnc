using System.Drawing;
using System.Windows.Forms;

namespace R4SoVNC.Server.Helpers
{
    public static class Theme
    {
        public static Color Background    = Color.FromArgb(10, 15, 30);
        public static Color Surface       = Color.FromArgb(18, 26, 50);
        public static Color SurfaceLight  = Color.FromArgb(24, 36, 70);
        public static Color Accent        = Color.FromArgb(0, 120, 215);
        public static Color AccentHover   = Color.FromArgb(0, 150, 255);
        public static Color AccentDark    = Color.FromArgb(0, 80, 160);
        public static Color Text          = Color.FromArgb(220, 230, 255);
        public static Color TextMuted     = Color.FromArgb(120, 140, 180);
        public static Color Success       = Color.FromArgb(0, 200, 120);
        public static Color Danger        = Color.FromArgb(220, 50, 50);
        public static Color Warning       = Color.FromArgb(230, 160, 20);
        public static Color Border        = Color.FromArgb(30, 50, 90);
        public static Color PanelDark     = Color.FromArgb(13, 20, 40);

        public static Font FontTitle      = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static Font FontSubtitle   = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static Font FontBody       = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font FontBold       = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font FontMono       = new Font("Consolas", 9f, FontStyle.Regular);
        public static Font FontSmall      = new Font("Segoe UI", 8f, FontStyle.Regular);

        public static void ApplyButton(Button btn, bool accent = true)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = accent ? Accent : SurfaceLight;
            btn.ForeColor = Text;
            btn.FlatAppearance.BorderColor = accent ? AccentDark : Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.Font = FontBold;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = accent ? AccentHover : Color.FromArgb(35, 55, 100);
        }

        public static void ApplyTextBox(TextBox tb)
        {
            tb.BackColor = PanelDark;
            tb.ForeColor = Text;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = FontBody;
        }

        public static void ApplyLabel(Label lbl, bool muted = false)
        {
            lbl.ForeColor = muted ? TextMuted : Text;
            lbl.BackColor = Color.Transparent;
            lbl.Font = muted ? FontSmall : FontBody;
        }

        public static void ApplyPanel(Panel p)
        {
            p.BackColor = Surface;
        }
    }
}
