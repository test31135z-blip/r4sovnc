using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace R4SoVNC.Client.Input
{
    public static class InputHandler
    {
        public static void ApplyMouseMove(byte[] data)
        {
            using var br = new BinaryReader(new MemoryStream(data));
            int x = br.ReadInt32();
            int y = br.ReadInt32();
            SetCursorPos(x, y);
        }

        public static void ApplyMouseClick(byte[] data)
        {
            using var br = new BinaryReader(new MemoryStream(data));
            int button = br.ReadInt32();
            int x = br.ReadInt32();
            int y = br.ReadInt32();
            bool down = br.ReadBoolean();

            SetCursorPos(x, y);

            uint dwFlags;
            if (button == 0)
                dwFlags = down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
            else if (button == 1)
                dwFlags = down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
            else
                dwFlags = down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;

            mouse_event(dwFlags, (uint)x, (uint)y, 0, UIntPtr.Zero);
        }

        public static void ApplyMouseScroll(byte[] data)
        {
            using var br = new BinaryReader(new MemoryStream(data));
            int delta = br.ReadInt32();
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
        }

        public static void ApplyKey(byte[] data, bool down)
        {
            using var br = new BinaryReader(new MemoryStream(data));
            int keyCode = br.ReadInt32();
            bool isDown = br.ReadBoolean();

            var input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.ki.wVk = (ushort)keyCode;
            input.ki.dwFlags = isDown ? 0u : KEYEVENTF_KEYUP;
            SendInput(1, ref input, Marshal.SizeOf(input));
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        private const uint MOUSEEVENTF_LEFTDOWN  = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP    = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP   = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN= 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP  = 0x0040;
        private const uint MOUSEEVENTF_WHEEL     = 0x0800;
        private const uint KEYEVENTF_KEYUP       = 0x0002;
        private const int  INPUT_KEYBOARD        = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk, wScan;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(4)] public MOUSEINPUT mi;
            [FieldOffset(4)] public KEYBDINPUT ki;
        }
    }
}
