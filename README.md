# QuickLaunch

A Windows 11 system tray app that recreates the Quick Launch toolbar from Windows 10. Left-click the tray icon to pop up a menu of your shortcuts; right-click to access settings or quit.

![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?logo=windows)
![PowerShell 7+](https://img.shields.io/badge/PowerShell-7%2B-5391FE?logo=powershell)

---

## Requirements

- Windows 10 or 11
- [PowerShell 7+](https://github.com/PowerShell/PowerShell/releases/latest)
- .NET Framework (pre-installed on Windows) — only needed to compile `QuickLaunch.exe`

---

## Setup

### 1. Build the launcher

Run the build script once to compile the `.exe` wrapper:

```powershell
pwsh -ExecutionPolicy Bypass -File .\build\Compile-Launcher.ps1
```

This produces `QuickLaunch.exe` in the project root and embeds `icons\icon.ico`.

### 2. Configure

Edit `settings.json` to point at your shortcuts folder and adjust appearance:

```json
{
  "ShortcutFolder": "C:\\Path\\To\\Your\\Shortcuts",
  "TrayIconPath":   "",
  "IconSize":       32,
  "ShowLabels":     true
}
```

| Setting | Description |
|---|---|
| `ShortcutFolder` | Folder containing `.lnk` shortcut files to display in the menu |
| `TrayIconPath` | Path to a custom `.ico` for the tray icon (leave empty to use the default) |
| `IconSize` | Icon size in pixels (e.g. `24`, `32`, `48`) |
| `ShowLabels` | `true` to show shortcut names next to icons, `false` for icons only |

### 3. Run

Double-click `QuickLaunch.exe`. The app will appear in the system tray.

To start automatically with Windows, create a shortcut to `QuickLaunch.exe` and place it in:

```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

---

## Usage

| Action | Result |
|---|---|
| Left-click tray icon | Opens the Quick Launch menu above the taskbar |
| Right-click tray icon → **Config** | Opens the shortcuts folder in Explorer |
| Right-click tray icon → **Edit Settings** | Opens `settings.json` in the default editor |
| Right-click tray icon → **Quit** | Exits the app |

Add, remove, or rename `.lnk` files in your `ShortcutFolder` to change what appears in the menu. The menu refreshes on each open.

---

## Development

### Lint

```powershell
Invoke-ScriptAnalyzer -Path . -Settings .vscode\PSScriptAnalyzerSettings.psd1 -Recurse
```

Or use the VS Code task: **Terminal → Run Task → Lint: Analyse all scripts**

### Build

```powershell
pwsh -ExecutionPolicy Bypass -File .\build\Compile-Launcher.ps1
```

Or use the VS Code task: **Terminal → Run Task → Build: Compile QuickLaunch.exe**

### Logs

Runtime logs are written to `Logs\QuickLaunch.log` and are useful for diagnosing shortcut launch failures or settings load errors.

---

## Project Structure

```
QuickLaunch/
├── QuickLaunch.ps1          # Main app — tray icon, WPF menu, shortcut launcher
├── QuickLaunch.exe          # Compiled launcher (not committed — rebuild as above)
├── settings.json            # User configuration
├── icons\                   # Icon assets
│   └── icon.ico
├── build\
│   └── Compile-Launcher.ps1 # Builds QuickLaunch.exe from a C# wrapper
├── .vscode\
│   ├── PSScriptAnalyzerSettings.psd1
│   └── tasks.json
└── Logs\                    # Runtime logs (not committed)
```
