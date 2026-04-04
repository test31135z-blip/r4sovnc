using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R4SoVNC.Server.Protocol;

namespace R4SoVNC.Server.Network
{
    public class ClientSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string ClientName { get; private set; } = "Unknown";
        public string IpAddress { get; }
        public DateTime ConnectedAt { get; } = DateTime.Now;
        public bool IsConnected { get; private set; } = true;

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();

        public event Action<ClientSession, Packet>? PacketReceived;
        public event Action<ClientSession>? Disconnected;

        private readonly object _writeLock = new();

        public ClientSession(TcpClient client)
        {
            _tcpClient = client;
            _stream = client.GetStream();
            IpAddress = client.Client.RemoteEndPoint?.ToString() ?? "?";
        }

        public void Start()
        {
            Task.Run(ReceiveLoop, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            try
            {
                byte[] header = new byte[4];
                while (!_cts.IsCancellationRequested)
                {
                    int read = 0;
                    while (read < 4)
                    {
                        int r = await _stream.ReadAsync(header, read, 4 - read, _cts.Token);
                        if (r == 0) goto Disconnected;
                        read += r;
                    }
                    var packet = Packet.Deserialize(header, _stream);
                    if (packet == null) goto Disconnected;

                    if (packet.Type == PacketType.ClientInfo)
                        ClientName = packet.GetDataAsString();

                    PacketReceived?.Invoke(this, packet);
                }
            }
            catch { }

            Disconnected:
            Disconnect();
        }

        public void SendPacket(Packet packet)
        {
            if (!IsConnected) return;
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
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            _cts.Cancel();
            try { _tcpClient.Close(); } catch { }
            Disconnected?.Invoke(this);
        }
    }
}
