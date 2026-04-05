using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using R4SoVNC.ClientEmbed.Network;
using R4SoVNC.ClientEmbed.Protocol;

namespace R4SoVNC.ClientEmbed.FileTransfer
{
    internal class FileHandler
    {
        private readonly ServerConnection _conn;
        private string?      _uploadPath;
        private FileStream?  _uploadStream;

        public FileHandler(ServerConnection conn) => _conn = conn;

        public void HandleFileListRequest(string dirPath)
        {
            try
            {
                if (string.IsNullOrEmpty(dirPath)) dirPath = @"C:\";
                var items = new List<object>();
                foreach (var d in Directory.GetDirectories(dirPath))
                    items.Add(new { name = Path.GetFileName(d), isDir = true, size = 0L });
                foreach (var f in Directory.GetFiles(dirPath))
                {
                    var fi = new FileInfo(f);
                    items.Add(new { name = fi.Name, isDir = false, size = fi.Length });
                }
                _conn.Send(new Packet(PacketType.FileListResponse, JsonConvert.SerializeObject(new { path = dirPath, items })));
            }
            catch (Exception ex)
            {
                _conn.Send(new Packet(PacketType.FileListResponse, JsonConvert.SerializeObject(new { error = ex.Message })));
            }
        }

        public void HandleDownloadRequest(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var info = new FileInfo(path);
                _conn.Send(new Packet(PacketType.FileDownloadData,
                    System.Text.Encoding.UTF8.GetBytes(info.Name)));
                using var fs = File.OpenRead(path);
                var buf = new byte[65536];
                int read;
                while ((read = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    var chunk = new byte[read];
                    Array.Copy(buf, chunk, read);
                    _conn.Send(new Packet(PacketType.FileDownloadData, chunk));
                }
                _conn.Send(new Packet(PacketType.FileDownloadDone));
            }
            catch { }
        }

        public void HandleUploadRequest(byte[] data)
        {
            string path = System.Text.Encoding.UTF8.GetString(data);
            try
            {
                _uploadPath   = path;
                _uploadStream = File.Create(path);
            }
            catch { _uploadStream = null; }
        }

        public void HandleUploadData(byte[] data)
        {
            try { _uploadStream?.Write(data, 0, data.Length); } catch { }
        }

        public void HandleUploadComplete()
        {
            _uploadStream?.Flush();
            _uploadStream?.Dispose();
            _uploadStream = null;
        }
    }
}
