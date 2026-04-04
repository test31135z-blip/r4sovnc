using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using R4SoVNC.Server.Helpers;
using R4SoVNC.Server.Network;
using R4SoVNC.Server.Protocol;

namespace R4SoVNC.Server.Forms
{
    public partial class FileTransferForm : Form
    {
        private readonly ClientSession _session;
        private string _remotePath = @"C:\";
        private FileStream? _downloadStream;
        private string? _downloadTarget;
        private long _downloadTotal;
        private long _downloadReceived;

        private FileStream? _uploadStream;
        private long _uploadTotal;
        private long _uploadSent;

        public FileTransferForm(ClientSession session)
        {
            _session = session;
            InitializeComponent();

            listRemote.AllowDrop = true;
            listLocal.ItemDrag += ListLocal_ItemDrag;
            listRemote.DragEnter += ListRemote_DragEnter;
            listRemote.DragDrop += ListRemote_DragDrop;

            listRemote.DragOver += (s, e) =>
                e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                    ? DragDropEffects.Copy : DragDropEffects.None;

            LoadLocalDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        public void LoadFileList(string json)
        {
            if (InvokeRequired) { Invoke(() => LoadFileList(json)); return; }
            try
            {
                var items = JsonConvert.DeserializeObject<List<RemoteFileItem>>(json);
                listRemote.Items.Clear();
                if (items == null) return;
                foreach (var item in items)
                {
                    var lvi = new ListViewItem(item.Name);
                    lvi.SubItems.Add(item.IsDirectory ? "<DIR>" : FormatSize(item.Size));
                    lvi.SubItems.Add(item.Modified);
                    lvi.Tag = item;
                    lvi.ForeColor = item.IsDirectory ? Theme.Accent : Theme.Text;
                    lvi.ImageIndex = item.IsDirectory ? 0 : 1;
                    listRemote.Items.Add(lvi);
                }
                lblRemotePath.Text = _remotePath;
            }
            catch { }
        }

        public void HandleIncoming(Packet packet)
        {
            if (packet.Type == PacketType.FileDownloadData)
            {
                if (_downloadStream == null) return;
                _downloadStream.Write(packet.Data);
                _downloadReceived += packet.Data.Length;
                int pct = (int)((_downloadReceived * 100) / Math.Max(1, _downloadTotal));
                this.Invoke(() =>
                {
                    progressTransfer.Value = Math.Min(100, pct);
                    lblTransferStatus.Text = $"Downloading: {FormatSize(_downloadReceived)} / {FormatSize(_downloadTotal)}";
                });
            }
            else if (packet.Type == PacketType.FileDownloadDone)
            {
                _downloadStream?.Close();
                _downloadStream = null;
                this.Invoke(() =>
                {
                    progressTransfer.Value = 100;
                    lblTransferStatus.Text = $"Download complete: {Path.GetFileName(_downloadTarget)}";
                    LoadLocalDirectory(Path.GetDirectoryName(_downloadTarget)!);
                });
            }
        }

        private void ListLocal_ItemDrag(object? s, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem lvi && lvi.Tag is string path)
            {
                listLocal.DoDragDrop(new DataObject(DataFormats.FileDrop,
                    new[] { path }), DragDropEffects.Copy);
            }
        }

        private void ListRemote_DragEnter(object? s, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ListRemote_DragDrop(object? s, DragEventArgs e)
        {
            var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            foreach (var f in files)
            {
                if (File.Exists(f)) UploadFile(f);
            }
        }

        private void UploadFile(string localPath)
        {
            string fileName = Path.GetFileName(localPath);
            long size = new FileInfo(localPath).Length;
            _uploadTotal = size;
            _uploadSent = 0;

            var reqPkt = PacketBuilder.FileUploadRequest(
                _remotePath.TrimEnd('\\') + "\\" + fileName, size);
            _session.SendPacket(reqPkt);

            const int chunkSize = 65536;
            byte[] buf = new byte[chunkSize];
            using var fs = File.OpenRead(localPath);
            int read;
            while ((read = fs.Read(buf, 0, chunkSize)) > 0)
            {
                byte[] chunk = new byte[read];
                Array.Copy(buf, chunk, read);
                _session.SendPacket(new Packet(PacketType.FileUploadData, chunk));
                _uploadSent += read;
                int pct = (int)((_uploadSent * 100) / _uploadTotal);
                this.Invoke(() =>
                {
                    progressTransfer.Value = Math.Min(100, pct);
                    lblTransferStatus.Text = $"Uploading: {FormatSize(_uploadSent)} / {FormatSize(_uploadTotal)}";
                });
            }
            _session.SendPacket(new Packet(PacketType.FileUploadComplete));
            this.Invoke(() =>
            {
                lblTransferStatus.Text = "Upload complete.";
                progressTransfer.Value = 100;
                _session.SendPacket(PacketBuilder.FileListRequest(_remotePath));
            });
        }

        private void LoadLocalDirectory(string path)
        {
            if (!Directory.Exists(path)) return;
            listLocal.Items.Clear();
            lblLocalPath.Text = path;

            try
            {
                foreach (var d in Directory.GetDirectories(path))
                {
                    var lvi = new ListViewItem(Path.GetFileName(d));
                    lvi.SubItems.Add("<DIR>");
                    lvi.Tag = d;
                    lvi.ForeColor = Theme.Accent;
                    listLocal.Items.Add(lvi);
                }
                foreach (var f in Directory.GetFiles(path))
                {
                    var lvi = new ListViewItem(Path.GetFileName(f));
                    lvi.SubItems.Add(FormatSize(new FileInfo(f).Length));
                    lvi.Tag = f;
                    lvi.ForeColor = Theme.Text;
                    listLocal.Items.Add(lvi);
                }
            }
            catch { }
        }

        private void listRemote_DoubleClick(object sender, EventArgs e)
        {
            if (listRemote.SelectedItems.Count == 0) return;
            var item = (RemoteFileItem)listRemote.SelectedItems[0].Tag!;
            if (item.IsDirectory)
            {
                _remotePath = item.FullPath;
                _session.SendPacket(PacketBuilder.FileListRequest(_remotePath));
            }
            else
            {
                // Download
                using var sfd = new SaveFileDialog { FileName = item.Name };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                _downloadTarget = sfd.FileName;
                _downloadTotal = item.Size;
                _downloadReceived = 0;
                _downloadStream = File.Create(_downloadTarget);
                _session.SendPacket(PacketBuilder.FileDownloadRequest(item.FullPath));
                lblTransferStatus.Text = $"Downloading {item.Name}...";
            }
        }

        private void listLocal_DoubleClick(object sender, EventArgs e)
        {
            if (listLocal.SelectedItems.Count == 0) return;
            string path = (string)listLocal.SelectedItems[0].Tag!;
            if (Directory.Exists(path))
                LoadLocalDirectory(path);
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (listLocal.SelectedItems.Count == 0) return;
            string path = (string)listLocal.SelectedItems[0].Tag!;
            if (File.Exists(path))
                System.Threading.Tasks.Task.Run(() => UploadFile(path));
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (listRemote.SelectedItems.Count == 0) return;
            var item = (RemoteFileItem)listRemote.SelectedItems[0].Tag!;
            using var sfd = new SaveFileDialog { FileName = item.Name };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            _downloadTarget = sfd.FileName;
            _downloadTotal = item.Size;
            _downloadReceived = 0;
            _downloadStream = File.Create(_downloadTarget);
            _session.SendPacket(PacketBuilder.FileDownloadRequest(item.FullPath));
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _session.SendPacket(PacketBuilder.FileListRequest(_remotePath));
        }

        private void btnNavUp_Click(object sender, EventArgs e)
        {
            var parent = Path.GetDirectoryName(_remotePath);
            if (!string.IsNullOrEmpty(parent))
            {
                _remotePath = parent;
                _session.SendPacket(PacketBuilder.FileListRequest(_remotePath));
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024:F1} MB";
            return $"{bytes / 1024.0 / 1024 / 1024:F2} GB";
        }
    }

    public class RemoteFileItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string Modified { get; set; } = "";
    }
}
