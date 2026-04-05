using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using R4SoVNC.ClientEmbed.Protocol;

namespace R4SoVNC.ClientEmbed.Network
{
    internal class ServerConnection
    {
        private TcpClient?     _tcp;
        private NetworkStream? _stream;
        private readonly object _lock = new();

        public bool IsConnected => _tcp?.Connected == true;

        public event Action<Packet>? PacketReceived;
        public event Action?         Disconnected;

        public bool Connect(string host, int port)
        {
            try
            {
                _tcp    = new TcpClient();
                _tcp.Connect(host, port);
                _stream = _tcp.GetStream();
                Task.Run(ReadLoop);
                return true;
            }
            catch { return false; }
        }

        public void Send(Packet p)
        {
            if (_stream == null) return;
            lock (_lock)
            {
                try { byte[] data = p.Serialize(); _stream.Write(data, 0, data.Length); }
                catch { Disconnect(); }
            }
        }

        public void Disconnect()
        {
            try { _tcp?.Close(); } catch { }
            _tcp    = null;
            _stream = null;
            Disconnected?.Invoke();
        }

        private void ReadLoop()
        {
            try
            {
                while (IsConnected && _stream != null)
                {
                    var pkt = Packet.Deserialize(_stream);
                    if (pkt == null) { Disconnect(); return; }
                    PacketReceived?.Invoke(pkt);
                }
            }
            catch { Disconnect(); }
        }
    }
}
