using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace R4SoVNC.Server.Network
{
    public class R4VNCServer
    {
        private TcpListener? _listener;
        private CancellationTokenSource _cts = new();
        public int Port { get; private set; }
        public bool IsRunning { get; private set; }

        public ConcurrentDictionary<string, ClientSession> Sessions { get; } = new();

        public event Action<ClientSession>? ClientConnected;
        public event Action<ClientSession>? ClientDisconnected;
        public event Action<string>? LogMessage;

        public void Start(int port)
        {
            Port = port;
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;
            LogMessage?.Invoke($"Server listening on port {port}");
            Task.Run(AcceptLoop, _cts.Token);
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var tcp = await _listener!.AcceptTcpClientAsync(_cts.Token);
                    tcp.NoDelay = true;
                    var session = new ClientSession(tcp);
                    Sessions[session.SessionId] = session;

                    session.Disconnected += s =>
                    {
                        Sessions.TryRemove(s.SessionId, out _);
                        LogMessage?.Invoke($"Client disconnected: {s.IpAddress}");
                        ClientDisconnected?.Invoke(s);
                    };

                    session.Start();
                    LogMessage?.Invoke($"Client connected: {tcp.Client.RemoteEndPoint}");
                    ClientConnected?.Invoke(session);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { LogMessage?.Invoke($"Accept error: {ex.Message}"); }
            }
        }

        public void Stop()
        {
            IsRunning = false;
            _cts.Cancel();
            foreach (var s in Sessions.Values) s.Disconnect();
            Sessions.Clear();
            _listener?.Stop();
            LogMessage?.Invoke("Server stopped.");
        }
    }
}
