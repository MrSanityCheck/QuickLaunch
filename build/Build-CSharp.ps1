$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "..\src"
$OutputDir  = Join-Path $ScriptDir "..\bin"

Write-Host "Building QuickLaunch..." -ForegroundColor Cyan

dotnet publish "$ProjectDir\QuickLaunch.csproj" `
    -c Release `
    --nologo `
    -o $OutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded. Output: $OutputDir" -ForegroundColor Green
} else {
    Write-Host "Build failed (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}
