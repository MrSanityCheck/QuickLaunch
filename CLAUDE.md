# CLAUDE.md — QuickLaunch

## Project Purpose

A Windows 11 system tray app that recreates the Quick Launch toolbar removed in Windows 11.
Left-clicking the tray icon pops up a floating shortcut menu above the taskbar; right-clicking gives an exit/settings menu.

---

## Technology Stack

- **Language**: C# 12 / .NET 8 (Windows)
- **UI**: WPF (popup window) + WinForms (tray icon via `NotifyIcon`)
- **Transparency**: `SetWindowCompositionAttribute` with `ACCENT_ENABLE_ACRYLICBLURBEHIND` — works with `AllowsTransparency="True"` layered windows
- **Settings**: JSON file at `%AppData%\QuickLaunch\settings.json`, live-reloaded via `FileSystemWatcher`

---

## Project Structure

```text
QuickLaunch/
├── src/
│   ├── QuickLaunch.csproj       # .NET 8 WinExe; UseWPF + UseWindowsForms
│   ├── App.xaml / App.xaml.cs   # Entry point; loads settings, wires tray, starts FileSystemWatcher
│   ├── Models/
│   │   └── AppSettings.cs       # Settings model; Load / Save / Reload; DefaultPath = %AppData%\QuickLaunch\settings.json
│   ├── Services/
│   │   ├── Logger.cs            # Timestamped log to bin\Logs\QuickLaunch.log
│   │   ├── ShortcutService.cs   # Scans ShortcutFolder for .lnk files; extracts icons via GDI32
│   │   └── ThemeDetector.cs     # Reads AppsUseLightTheme registry key
│   ├── Tray/
│   │   └── TrayIconController.cs # NotifyIcon; left-click → popup; right-click → context menu
│   └── UI/
│       ├── PopupWindow.xaml/cs  # Acrylic WPF popup; positions above tray icon on Loaded
│       └── NativeMethods.cs     # P/Invokes: SetWindowCompositionAttribute, SetWindowLong, DeleteObject
├── icons/
│   └── icon.ico                 # Default tray icon (also embedded via ApplicationIcon in csproj)
├── build/
│   └── Build-CSharp.ps1         # dotnet publish to bin\ (Release)
├── .vscode/
│   └── tasks.json               # "Build: dotnet build" (Ctrl+Shift+B) — kills running instance first
└── CLAUDE.md
```

---

## Settings (`%AppData%\QuickLaunch\settings.json`)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ShortcutFolder` | string | `C:\CDS\Portable\CDS` | Folder of `.lnk` files to display |
| `TrayIconPath` | string | `""` | Custom `.ico` path; empty uses `icons\icon.ico` |
| `IconSize` | int | `32` | Shortcut icon size in pixels |
| `ShowLabels` | bool | `true` | Show shortcut name next to icon |
| `WindowOpacity` | double | `0.85` | Acrylic tint strength (0.0 = pure blur, 1.0 = solid) |
| `EnableLogging` | bool | `true` | Write to `bin\Logs\QuickLaunch.log`; set `false` to silence logging |

Settings are seeded with defaults on first run and live-reloaded on save (300 ms debounce).

---

## Coding Standards

### C#

- **4-space indentation**, Allman-adjacent — opening brace on same line for methods/types
- **Nullable enabled** — no `!` suppression without a comment explaining why
- **No comments on obvious code** — only document non-obvious constraints or Win32 quirks
- **Error handling**: wrap all external calls (file I/O, P/Invoke, process launch) in `try/catch` and log

### Namespace collision workaround

Both `UseWPF` and `UseWindowsForms` are enabled, so implicit usings create ambiguities. Resolve with file-level aliases in affected files:

```csharp
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Timer = System.Threading.Timer;
```

### Acrylic transparency

`AllowsTransparency="True"` creates a `WS_EX_LAYERED` HWND. The `DWMSBT_TRANSIENTWINDOW` DWM API **does not work** with layered windows. Use `SetWindowCompositionAttribute` + `ACCENT_ENABLE_ACRYLICBLURBEHIND` instead. The `WindowOpacity` setting maps to the alpha byte of `AccentPolicy.GradientColor`.

### Logging

```csharp
var logger = new Logger(Path.Combine(AppContext.BaseDirectory, "Logs"));
logger.Log("message");  // written to bin\Logs\QuickLaunch.log
```

---

## Build

```powershell
# Debug (fast, used during development)
dotnet build src\QuickLaunch.csproj

# Release (publish to bin\)
pwsh -File build\Build-CSharp.ps1
```

The VS Code task **Ctrl+Shift+B** runs `dotnet build` and kills any running instance first.

---

## Gotchas

- **CRLF**: all files use CRLF — `.gitattributes` enforces this.
- **File lock**: `bin\QuickLaunch.exe` is locked while the app is running. The build task handles this; if building manually, quit via systray first.
- **Icon copy**: `icons\` is declared as `<Content CopyToOutputDirectory="PreserveNewest">` in the csproj — the icon must exist at `bin\icons\icon.ico` at runtime.
- **Settings path**: the app reads from `%AppData%\QuickLaunch\settings.json`, not from `bin\`. The root `settings.json` (if present) is the old PowerShell prototype and is ignored.

---

## What Does Not Belong Here

- Per-user shortcut `.lnk` files (live in the configured `ShortcutFolder`, outside the repo)
- Compiled output (`bin\`, `src\obj\`) — gitignored
- Runtime logs (`Logs\`) — gitignored
