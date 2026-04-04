# R4SoVNC — Remote Access & Control

A full-featured AnyDesk/TeamViewer-like remote desktop application built with C# .NET 6 Windows Forms.

## Features

- **Full Remote Desktop Control** — Real-time screen streaming at ~30 FPS (JPEG compressed)
- **Mouse & Keyboard Control** — Complete mouse (move, click, scroll) and keyboard passthrough
- **File Manager** — Browse, upload, and download files with drag & drop support
- **Client Builder** — Build custom client executables with embedded host/port config
- **Dark Blue Modern UI** — Professional dark theme with modern Windows Forms design
- **Multi-client Support** — Connect to and manage multiple clients simultaneously
- **TCP-based Protocol** — Reliable binary packet protocol over TCP

## Architecture

```
R4SoVNC/
├── R4SoVNC.Server/          # Windows Forms server application
│   ├── Forms/
│   │   ├── MainForm         # Main server UI, client list
│   │   ├── ViewerForm       # Remote screen viewer + control
│   │   ├── BuilderForm      # Client EXE builder
│   │   └── FileTransferForm # File manager (local ↔ remote)
│   ├── Network/
│   │   ├── R4VNCServer      # TCP listener
│   │   └── ClientSession    # Per-client connection handler
│   └── Protocol/
│       └── PacketProtocol   # Binary packet definitions
│
└── R4SoVNC.Client/          # Console app (deployed to target machines)
    ├── Capture/
    │   └── ScreenCapturer   # Screen capture + JPEG compression
    ├── Input/
    │   └── InputHandler     # Mouse/keyboard simulation (Win32 API)
    ├── FileTransfer/
    │   └── FileHandler      # File list, upload, download
    └── Network/
        └── ServerConnection # TCP connection to server
```

## Requirements

- Windows 10/11 (x64)
- .NET 6.0 SDK (for building)
- .NET 6.0 Runtime (for running — or publish self-contained)
- Visual Studio 2022 recommended

## Quick Start

### 1. Open Solution
Open `r4sovnc.sln` in Visual Studio 2022.

### 2. Build & Run Server
Set `R4SoVNC.Server` as startup project and run (F5).

### 3. Configure Server
- Enter a port number (default: 7890)
- Click **▶ Start Server**

### 4. Build a Client
- Click **⚙ Client Builder**
- Enter your server's IP/hostname and port
- Enter output filename (e.g. `r4client.exe`)
- Click **⚙ Build Client**
- Distribute the built EXE to the target machine

### 5. Connect
- When a client runs and connects, it appears in the **Connected Clients** list
- Double-click a client or select it and click **🖥 Connect / View**
- In the viewer, click **🟢 Start Control** to take control
- Click **📁 File Manager** to browse and transfer files

## Packet Protocol

| Type | ID | Description |
|------|----|-------------|
| Heartbeat | 0x01 | Keep-alive |
| ScreenData | 0x02 | JPEG frame |
| MouseMove | 0x03 | x, y coordinates |
| MouseClick | 0x04 | button, x, y, down |
| MouseScroll | 0x05 | scroll delta |
| KeyDown/Up | 0x06/07 | VK code |
| FileListRequest | 0x08 | path string |
| FileListResponse | 0x09 | JSON array |
| FileUpload | 0x0A-0C | request, data, complete |
| FileDownload | 0x0D-0F | request, data, done |
| ClientInfo | 0x10 | machine name |
| Disconnect | 0x11 | graceful disconnect |

## File Transfer Usage

1. Click **📁 File Manager** in the viewer toolbar
2. Left panel = your local files | Right panel = remote client files
3. **Upload:** Select a local file → click **⬆ Upload →** (or drag from local to remote)
4. **Download:** Select a remote file → click **⬇ Download ←** (or double-click)
5. Navigate folders by double-clicking directories
6. Use **↑ Up** to go to parent directory, **↻ Refresh** to reload

## Building Self-Contained Client

```bash
dotnet publish R4SoVNC.Client -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Notes

- Firewall: Allow the chosen port (default 7890) through Windows Firewall
- For internet connections: use your public IP or a domain name in the builder
- Screen quality can be adjusted in `ScreenCapturer` (default: 50% JPEG quality)
- The client runs silently in the background (console hidden in release builds)

## License

MIT License — free to use and modify.
