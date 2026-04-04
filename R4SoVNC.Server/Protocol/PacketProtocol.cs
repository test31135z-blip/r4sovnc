using System;
using System.IO;
using System.Text;

namespace R4SoVNC.Server.Protocol
{
    public enum PacketType : byte
    {
        Heartbeat         = 0x01,
        ScreenData        = 0x02,
        MouseMove         = 0x03,
        MouseClick        = 0x04,
        MouseScroll       = 0x05,
        KeyDown           = 0x06,
        KeyUp             = 0x07,
        FileListRequest   = 0x08,
        FileListResponse  = 0x09,
        FileUploadRequest = 0x0A,
        FileUploadData    = 0x0B,
        FileUploadComplete= 0x0C,
        FileDownloadReq   = 0x0D,
        FileDownloadData  = 0x0E,
        FileDownloadDone  = 0x0F,
        ClientInfo        = 0x10,
        Disconnect        = 0x11,
        // Microphone
        MicStart          = 0x12,
        MicStop           = 0x13,
        AudioData         = 0x14,
        // Camera
        CamStart          = 0x15,
        CamStop           = 0x16,
        CameraFrame       = 0x17,
    }

    public class Packet
    {
        public PacketType Type { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public Packet(PacketType type, byte[] data) { Type = type; Data = data; }
        public Packet(PacketType type, string text) { Type = type; Data = Encoding.UTF8.GetBytes(text); }
        public Packet(PacketType type) { Type = type; Data = Array.Empty<byte>(); }

        public string GetDataAsString() => Encoding.UTF8.GetString(Data);

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write((int)(Data.Length + 1));
            bw.Write((byte)Type);
            bw.Write(Data);
            return ms.ToArray();
        }

        public static Packet? Deserialize(byte[] header4, Stream stream)
        {
            int length = BitConverter.ToInt32(header4, 0);
            if (length <= 0 || length > 15_000_000) return null;
            byte[] body = new byte[length];
            int read = 0;
            while (read < length)
            {
                int r = stream.Read(body, read, length - read);
                if (r == 0) return null;
                read += r;
            }
            PacketType type = (PacketType)body[0];
            byte[] data = new byte[length - 1];
            Array.Copy(body, 1, data, 0, data.Length);
            return new Packet(type, data);
        }
    }

    public static class PacketBuilder
    {
        public static Packet MouseMove(int x, int y)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(x); bw.Write(y);
            return new Packet(PacketType.MouseMove, ms.ToArray());
        }

        public static Packet MouseClick(int button, int x, int y, bool down)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(button); bw.Write(x); bw.Write(y); bw.Write(down);
            return new Packet(PacketType.MouseClick, ms.ToArray());
        }

        public static Packet MouseScroll(int delta)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(delta);
            return new Packet(PacketType.MouseScroll, ms.ToArray());
        }

        public static Packet KeyEvent(int keyCode, bool down)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(keyCode); bw.Write(down);
            return new Packet(down ? PacketType.KeyDown : PacketType.KeyUp, ms.ToArray());
        }

        public static Packet FileListRequest(string path) => new Packet(PacketType.FileListRequest, path);
        public static Packet FileDownloadRequest(string remotePath) => new Packet(PacketType.FileDownloadReq, remotePath);

        public static Packet FileUploadRequest(string fileName, long fileSize)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(fileName); bw.Write(fileSize);
            return new Packet(PacketType.FileUploadRequest, ms.ToArray());
        }

        public static Packet MicStart() => new Packet(PacketType.MicStart);
        public static Packet MicStop()  => new Packet(PacketType.MicStop);
        public static Packet CamStart() => new Packet(PacketType.CamStart);
        public static Packet CamStop()  => new Packet(PacketType.CamStop);
    }
}
