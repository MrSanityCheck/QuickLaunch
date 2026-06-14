# QuickLaunch

A Windows 11 system tray app that recreates the Quick Launch toolbar removed in Windows 11.

Left-click the tray icon to pop up a floating shortcut menu above the taskbar.
Right-click for settings and quit.

![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?logo=windows)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)

---

## Install

Run this in PowerShell (no .NET install required — the runtime is bundled):

```powershell
irm https://raw.githubusercontent.com/MrSanityCheck/QuickLaunch/master/install.ps1 | iex
```

This downloads the latest release, extracts it to `%LOCALAPPDATA%\QuickLaunch\`, and adds a startup shortcut so it launches automatically on login. Re-run at any time to update.

---

## Requirements (dev builds only)

- Windows 11 (Windows 10 works but acrylic effect may differ)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — not needed if you installed via the one-liner above

---

## Setup

### 1. Build

```powershell
# Debug build (output to bin\)
dotnet build src\QuickLaunch.csproj

# Release build (optimised, output to bin\)
pwsh -File build\Build-CSharp.ps1
```

Or use **Ctrl+Shift+B** in VS Code (kills any running instance automatically).

### 2. Configure

On first run, settings are written to `%AppData%\QuickLaunch\settings.json`.
Right-click the tray icon → **Edit Settings** to open the file directly.

```json
{
  "ShortcutFolder": "C:\\Path\\To\\Your\\Shortcuts",
  "TrayIconPath":   "",
  "IconSize":       32,
  "ShowLabels":     true,
  "WindowOpacity":  0.85
}
```

| Setting | Description |
|---|---|
| `ShortcutFolder` | Folder containing `.lnk` shortcut files to display |
| `TrayIconPath` | Path to a custom `.ico` for the tray icon (empty = built-in icon) |
| `IconSize` | Icon size in pixels — `24`, `32`, `48` etc. |
| `ShowLabels` | `true` to show names next to icons, `false` for icons only |
| `WindowOpacity` | Acrylic tint strength: `0.0` = pure blur, `1.0` = solid background |

Settings are reloaded automatically on save — no restart needed.

### 3. Run

Double-click `bin\QuickLaunch.exe`.

**Auto-start with Windows:** create a shortcut to `bin\QuickLaunch.exe` and place it in:

```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

---

## Usage

| Action | Result |
|---|---|
| Left-click tray icon | Opens the Quick Launch menu above the taskbar |
| Right-click → **Config** | Opens the shortcuts folder in Explorer |
| Right-click → **Edit Settings** | Opens `settings.json` in the default editor |
| Right-click → **Quit** | Exits the app |

Add, remove, or rename `.lnk` files in your `ShortcutFolder` to change what appears in the menu. The menu re-reads the folder on every open.

---

## Development

### Build

```powershell
dotnet build src\QuickLaunch.csproj        # debug
pwsh -File build\Build-CSharp.ps1          # release publish
```

### Logs

Runtime logs are written to `bin\Logs\QuickLaunch.log`.

### Project structure

```text
QuickLaunch/
├── src/
│   ├── QuickLaunch.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── Models/AppSettings.cs
│   ├── Services/          # Logger, ShortcutService, ThemeDetector
│   ├── Tray/              # TrayIconController (NotifyIcon)
│   └── UI/                # PopupWindow, NativeMethods
├── icons/
│   └── icon.ico
├── build/
│   └── Build-CSharp.ps1
└── .vscode/
    └── tasks.json
```
