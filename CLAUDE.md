# CLAUDE.md — Windows Post-Install Configuration

## Project Purpose

An app that sits in teh systray that launches a menu that behaves like the Windows 10 Quick Launch feature that is no longer in Windows 11. Want the code to be predominantly PowerShell 7+ apart from where it is unavoidable to have to use other technologies such as .net.

---

## Project Requirements

- **Look & Feel**: Needs to present a modern windows 11 acrylic style interface.
- **Configurable**: Some key configuration Options:
    - Need to be able to configure the icon for the app window and systray
    - Need the window to list shortcuts from a user-defined folder
    - User to be able to configure size of icons
    - User to be able to configure whether the name of the shortcuts show next to teh icons
    - Settings stored in editable .json file
- **Behaviour**: 
    - Left-clicking the systray icon brings up the window directly above it
    Right clicking the systray icon just to provide an exit option



---

## Project Structure

```text
QuickLaunch/
├── QuickLaunch.ps1          # Main application — tray icon, WPF menu, shortcut launcher
├── QuickLaunch.exe          # Compiled launcher (gitignored — rebuild via build\Compile-Launcher.ps1)
├── settings.json            # User configuration (ShortcutFolder, TrayIconPath, IconSize, ShowLabels)
├── icons\                   # Icon assets
│   └── icon.ico             # Default tray/window icon
├── build\
│   └── Compile-Launcher.ps1 # Compiles QuickLaunch.exe (C# wrapper that starts the PS1 hidden)
├── .gitignore
├── .vscode\
│   ├── PSScriptAnalyzerSettings.psd1   # Linter rules
│   └── tasks.json                      # VS Code tasks: Lint and Build
├── Logs\                    # Runtime log files (gitignored)
│   └── QuickLaunch.log
└── CLAUDE.md
```

---

## Coding Standards

### PowerShell

- **4-space indentation**, OTBS brace style (opening brace on same line)
- **Cmdlet casing**: `Get-LocalUser`, `Set-NetConnectionProfile` — correct PowerShell casing always
- **Error handling**: every operation wrapped in `try/catch`; log both success and failure explicitly
- **No hardcoded versions**: use the GitHub releases API to resolve latest versions dynamically
- **Log directory guard**: always create the log directory before the first `Write-Log` call
- **PowerShell 7**: PowerShell 7+ minimum version to take advantage of more recent features

### Logging pattern (use consistently across all scripts)

```powershell
$LogDir  = "C:\ShawSoft\Code\QuickLaunch\Logs"
$LogPath = "$LogDir\<ScriptName>.log"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp $Message" | Out-File -FilePath $LogPath -Append -Encoding UTF8
}
```


---

## Linting

PSScriptAnalyzer is configured via `.vscode/PSScriptAnalyzerSettings.psd1`.

Run from terminal:

```powershell
Invoke-ScriptAnalyzer -Path scripts\ -Settings .vscode\PSScriptAnalyzerSettings.psd1 -Recurse
```

Or via VS Code task: **Terminal → Run Task → Lint: Analyse all scripts**

---

## Gotchas

- **CRLF**: all files must use CRLF. `.gitattributes` enforces this. Do not introduce LF-only files.
- **Execution policy**: scripts are invoked with `-ExecutionPolicy Bypass`. Do not add `Set-ExecutionPolicy` calls inside scripts.

---

## What Does Not Belong Here

- Third-party icons or assets not used by the app
- Per-user shortcut files (those live in the configured `ShortcutFolder`, outside the repo)
- Compiled `.exe` or build artifacts (gitignored; regenerate with `Compile-Launcher.ps1`)
- Log files (gitignored; written to `Logs\` at runtime)