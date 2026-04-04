using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using R4SoVNC.Client.Network;
using R4SoVNC.Client.Protocol;

namespace R4SoVNC.Client.Capture
{
    public class AudioCapturer : IDisposable
    {
        private WaveInEvent? _waveIn;
        private readonly ServerConnection _conn;
        private bool _active;

        public AudioCapturer(ServerConnection conn)
        {
            _conn = conn;
        }

        public void Start()
        {
            if (_active) return;
            try
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(44100, 16, 1),
                    BufferMilliseconds = 50
                };
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.StartRecording();
                _active = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[R4SoVNC] Mic error: {ex.Message}");
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_active || e.BytesRecorded == 0) return;
            byte[] chunk = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, chunk, e.BytesRecorded);
            _conn.Send(new Packet(PacketType.AudioData, chunk));
        }

        public void Stop()
        {
            _active = false;
            try { _waveIn?.StopRecording(); } catch { }
            _waveIn?.Dispose();
            _waveIn = null;
        }

        public void Dispose() => Stop();
    }
}
