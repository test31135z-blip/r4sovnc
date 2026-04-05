using System;
using NAudio.Wave;
using R4SoVNC.ClientEmbed.Network;
using R4SoVNC.ClientEmbed.Protocol;

namespace R4SoVNC.ClientEmbed.Capture
{
    internal class AudioCapturer : IDisposable
    {
        private readonly ServerConnection _conn;
        private WaveInEvent? _wave;
        private bool _active;

        public AudioCapturer(ServerConnection conn) => _conn = conn;

        public void Start()
        {
            if (_active) return;
            _active = true;
            try
            {
                _wave = new WaveInEvent
                {
                    WaveFormat    = new WaveFormat(16000, 1),
                    BufferMilliseconds = 50
                };
                _wave.DataAvailable += (_, e) =>
                {
                    if (!_active || !_conn.IsConnected) return;
                    var data = new byte[e.BytesRecorded];
                    Buffer.BlockCopy(e.Buffer, 0, data, 0, e.BytesRecorded);
                    _conn.Send(new Packet(PacketType.AudioData, data));
                };
                _wave.StartRecording();
            }
            catch { _active = false; }
        }

        public void Stop()
        {
            _active = false;
            try { _wave?.StopRecording(); } catch { }
            _wave?.Dispose();
            _wave = null;
        }

        public void Dispose() => Stop();
    }
}
