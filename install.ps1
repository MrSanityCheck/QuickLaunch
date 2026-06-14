$ErrorActionPreference = 'Stop'

$repo       = "MrSanityCheck/QuickLaunch"
$installDir = "$env:LOCALAPPDATA\QuickLaunch"
$startupDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"

Write-Host "Fetching latest release..." -ForegroundColor Cyan
$release = irm "https://api.github.com/repos/$repo/releases/latest"
$asset   = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1
if (-not $asset) { throw "No zip asset found in latest release." }

Write-Host "Downloading $($asset.name)..." -ForegroundColor Cyan
$zipPath = Join-Path $env:TEMP $asset.name
irm $asset.browser_download_url -OutFile $zipPath

# Stop any running instance before replacing files
Stop-Process -Name QuickLaunch -ErrorAction SilentlyContinue

Write-Host "Installing to $installDir..." -ForegroundColor Cyan
if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
Expand-Archive -Path $zipPath -DestinationPath $installDir -Force
Remove-Item $zipPath

Write-Host "Creating startup shortcut..." -ForegroundColor Cyan
$wsh            = New-Object -ComObject WScript.Shell
$shortcut       = $wsh.CreateShortcut("$startupDir\QuickLaunch.lnk")
$shortcut.TargetPath      = "$installDir\QuickLaunch.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.Save()

Write-Host ""
Write-Host "Done. Launching QuickLaunch..." -ForegroundColor Green
Start-Process "$installDir\QuickLaunch.exe"
Write-Host "QuickLaunch will also start automatically on next login." -ForegroundColor Green
