using System;
using System.Runtime.InteropServices;

namespace R4SoVNC.ClientEmbed.Input
{
    internal static class InputHandler
    {
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern short GetSystemMetrics(int nIndex);

        const uint MOUSEEVENTF_MOVE       = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
        const uint MOUSEEVENTF_LEFTUP     = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;
        const uint MOUSEEVENTF_WHEEL      = 0x0800;
        const uint KEYEVENTF_KEYUP        = 0x0002;

        public static void ApplyMouseMove(byte[] data)
        {
            if (data.Length < 8) return;
            int x = BitConverter.ToInt32(data, 0);
            int y = BitConverter.ToInt32(data, 4);
            SetCursorPos(x, y);
        }

        public static void ApplyMouseClick(byte[] data)
        {
            if (data.Length < 9) return;
            int    x      = BitConverter.ToInt32(data, 0);
            int    y      = BitConverter.ToInt32(data, 4);
            byte   btn    = data[8];
            bool   down   = data.Length > 9 && data[9] == 1;
            SetCursorPos(x, y);
            uint downFlag = btn == 0 ? MOUSEEVENTF_LEFTDOWN : btn == 1 ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_MIDDLEDOWN;
            uint upFlag   = btn == 0 ? MOUSEEVENTF_LEFTUP   : btn == 1 ? MOUSEEVENTF_RIGHTUP   : MOUSEEVENTF_MIDDLEUP;
            mouse_event(down ? downFlag : upFlag, 0, 0, 0, 0);
        }

        public static void ApplyMouseScroll(byte[] data)
        {
            if (data.Length < 4) return;
            int delta = BitConverter.ToInt32(data, 0);
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta * 120, 0);
        }

        public static void ApplyKey(byte[] data, bool down)
        {
            if (data.Length < 1) return;
            byte vk = data[0];
            keybd_event(vk, 0, down ? 0u : KEYEVENTF_KEYUP, 0);
        }
    }
}
