using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using R4SoVNC.Client.Network;
using R4SoVNC.Client.Protocol;

namespace R4SoVNC.Client.FileTransfer
{
    public class FileHandler
    {
        private readonly ServerConnection _conn;
        private FileStream? _uploadStream;
        private string? _uploadPath;
        private long _uploadRemaining;

        private FileStream? _downloadStream;

        public FileHandler(ServerConnection conn)
        {
            _conn = conn;
        }

        public void HandleFileListRequest(string path)
        {
            if (!Directory.Exists(path)) path = @"C:\";
            var items = new List<RemoteFileItem>();
            try
            {
                foreach (var d in Directory.GetDirectories(path))
                {
                    var di = new DirectoryInfo(d);
                    items.Add(new RemoteFileItem
                    {
                        Name = di.Name,
                        FullPath = di.FullName,
                        IsDirectory = true,
                        Size = 0,
                        Modified = di.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
                foreach (var f in Directory.GetFiles(path))
                {
                    var fi = new FileInfo(f);
                    items.Add(new RemoteFileItem
                    {
                        Name = fi.Name,
                        FullPath = fi.FullName,
                        IsDirectory = false,
                        Size = fi.Length,
                        Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            catch { }

            string json = JsonConvert.SerializeObject(items);
            _conn.Send(new Packet(PacketType.FileListResponse, json));
        }

        public void HandleDownloadRequest(string remotePath)
        {
            if (!File.Exists(remotePath))
            {
                _conn.Send(new Packet(PacketType.FileDownloadDone));
                return;
            }
            const int chunkSize = 65536;
            byte[] buf = new byte[chunkSize];
            try
            {
                using var fs = File.OpenRead(remotePath);
                int read;
                while ((read = fs.Read(buf, 0, chunkSize)) > 0)
                {
                    byte[] chunk = new byte[read];
                    Array.Copy(buf, chunk, read);
                    _conn.Send(new Packet(PacketType.FileDownloadData, chunk));
                }
            }
            catch { }
            _conn.Send(new Packet(PacketType.FileDownloadDone));
        }

        public void HandleUploadRequest(byte[] data)
        {
            using var br = new BinaryReader(new MemoryStream(data));
            string remotePath = br.ReadString();
            long size = br.ReadInt64();
            _uploadPath = remotePath;
            _uploadRemaining = size;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(remotePath)!);
                _uploadStream = File.Create(remotePath);
            }
            catch { _uploadStream = null; }
        }

        public void HandleUploadData(byte[] chunk)
        {
            _uploadStream?.Write(chunk);
        }

        public void HandleUploadComplete()
        {
            _uploadStream?.Close();
            _uploadStream = null;
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
