$source = @"
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string ps1Path = Path.Combine(exeDir, `"QuickLaunch.ps1`");

        string pwsh = `"powershell.exe`";
        string pathEnv = Environment.GetEnvironmentVariable(`"PATH`") ?? `"`";
        foreach (string dir in pathEnv.Split(';'))
        {
            string candidate = Path.Combine(dir.Trim(), `"pwsh.exe`");
            if (File.Exists(candidate)) { pwsh = candidate; break; }
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = pwsh;
        startInfo.Arguments = `"-ExecutionPolicy Bypass -WindowStyle Hidden -File \`"`" + ps1Path + `"\`"`";
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        Process.Start(startInfo);
    }
}
"@

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ScriptDir) { $ScriptDir = $PWD.Path }
$ProjectDir = Split-Path -Parent $ScriptDir

$sourcePath = Join-Path $ScriptDir  "Launcher.cs"
$exePath    = Join-Path $ProjectDir "QuickLaunch.exe"
$iconPath   = Join-Path $ProjectDir "icons\icon.ico"

Set-Content -Path $sourcePath -Value $source -Encoding UTF8

$csc = (Get-ChildItem "C:\Windows\Microsoft.NET\Framework64\v*\csc.exe" | Sort-Object FullName -Descending | Select-Object -First 1).FullName

Write-Host "Compiling QuickLaunch.exe..."

if (Test-Path $iconPath) {
    & $csc /nologo /target:winexe "/win32icon:$iconPath" "/out:$exePath" $sourcePath
    Write-Host "`nSuccess! Compiled QuickLaunch.exe with icon embedded." -ForegroundColor Green
} else {
    & $csc /nologo /target:winexe "/out:$exePath" $sourcePath
    Write-Host "`nSuccess! Compiled QuickLaunch.exe. (No icon.ico found in icons\ — no icon embedded.)" -ForegroundColor Yellow
}

Remove-Item $sourcePath -Force
Start-Sleep -Seconds 3
