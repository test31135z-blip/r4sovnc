using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R4SoVNC.Client.Protocol;

namespace R4SoVNC.Client.Network
{
    public class ServerConnection
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource _cts = new();
        public bool IsConnected { get; private set; }

        public event Action<Packet>? PacketReceived;
        public event Action? Disconnected;

        private readonly object _writeLock = new();

        public bool Connect(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.Connect(host, port);
                _stream = _client.GetStream();
                IsConnected = true;
                _cts = new CancellationTokenSource();
                Task.Run(ReceiveLoop, _cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                byte[] header = new byte[4];
                while (!_cts.IsCancellationRequested && _stream != null)
                {
                    int read = 0;
                    while (read < 4)
                    {
                        int r = await _stream.ReadAsync(header, read, 4 - read, _cts.Token);
                        if (r == 0) goto Done;
                        read += r;
                    }
                    var pkt = Packet.Deserialize(header, _stream);
                    if (pkt == null) goto Done;
                    PacketReceived?.Invoke(pkt);
                }
            }
            catch { }
            Done:
            IsConnected = false;
            Disconnected?.Invoke();
        }

        public void Send(Packet packet)
        {
            if (!IsConnected || _stream == null) return;
            try
            {
                byte[] data = packet.Serialize();
                lock (_writeLock)
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
            }
            catch
            {
                IsConnected = false;
                Disconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            _cts.Cancel();
            try { _client?.Close(); } catch { }
        }
    }
}
