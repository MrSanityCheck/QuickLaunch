param(
    [switch]$Hidden
)

if (-not $Hidden) {
    $pwsh = (Get-Command pwsh.exe -ErrorAction SilentlyContinue)?.Source ?? "powershell.exe"
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $pwsh
    $startInfo.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$PSCommandPath`" -Hidden"
    $startInfo.CreateNoWindow = $true
    $startInfo.UseShellExecute = $false
    [System.Diagnostics.Process]::Start($startInfo) | Out-Null
    exit
}

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName WindowsBase

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogDir  = Join-Path $ScriptDir "Logs"
$LogPath = Join-Path $LogDir "QuickLaunch.log"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp $Message" | Out-File -FilePath $LogPath -Append -Encoding UTF8
}

$SettingsFile = Join-Path $ScriptDir "settings.json"
$Settings = @{
    ShortcutFolder = "C:\CDS\Portable\CDS"
    TrayIconPath   = ""
    IconSize       = 32
    ShowLabels     = $true
}

if (Test-Path $SettingsFile) {
    try {
        $LoadedSettings = Get-Content $SettingsFile -Raw | ConvertFrom-Json
        if ($LoadedSettings.ShortcutFolder) { $Settings.ShortcutFolder = $LoadedSettings.ShortcutFolder }
        if ($LoadedSettings.TrayIconPath)   { $Settings.TrayIconPath   = $LoadedSettings.TrayIconPath }
        if ($null -ne $LoadedSettings.IconSize) { $Settings.IconSize = [int]$LoadedSettings.IconSize }
        if ($null -ne $LoadedSettings.PSObject.Properties['ShowLabels']) { $Settings.ShowLabels = [bool]$LoadedSettings.ShowLabels }
        Write-Log "Settings loaded (ShortcutFolder=$($Settings.ShortcutFolder) IconSize=$($Settings.IconSize) ShowLabels=$($Settings.ShowLabels))"
    } catch {
        Write-Log "Failed to load settings.json: $_"
    }
}

if (-not (Test-Path $Settings.ShortcutFolder)) {
    try {
        New-Item -ItemType Directory -Path $Settings.ShortcutFolder -Force | Out-Null
        Write-Log "Created shortcut folder: $($Settings.ShortcutFolder)"
    } catch {
        Write-Log "Failed to create shortcut folder: $($Settings.ShortcutFolder) - $_"
    }
}

# Theme detection
$IsLightMode = $false
try {
    $RegistryPath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"
    $val = Get-ItemProperty -Path $RegistryPath -Name "AppsUseLightTheme" -ErrorAction SilentlyContinue
    if ($null -ne $val -and $val.AppsUseLightTheme -eq 1) {
        $IsLightMode = $true
    }
} catch {}

Write-Log "Theme: $(if ($IsLightMode) { 'Light' } else { 'Dark' })"
$BgColor = if ($IsLightMode) { "#F3F3F3" } else { "#202020" }
$FgColor = if ($IsLightMode) { "#000000" } else { "#FFFFFF" }
$HoverColor = if ($IsLightMode) { "#E0E0E0" } else { "#333333" }
$BorderColor = if ($IsLightMode) { "#CCCCCC" } else { "#333333" }

# Helper to convert Icon to ImageSource
function Get-ImageSourceFromIcon {
    param($IconPath, $ShortcutPath)
    
    $extractedIcon = $null
    
    # Try to extract icon from the target first if it's a shortcut
    try {
        $wshShell = New-Object -ComObject WScript.Shell
        $shortcut = $wshShell.CreateShortcut($ShortcutPath)
        $target = $shortcut.TargetPath
        if ($target -and (Test-Path $target)) {
            $extractedIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($target)
        }
    } catch {}

    # Fallback to the shortcut itself
    if ($null -eq $extractedIcon) {
        try {
            $extractedIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($IconPath)
        } catch {}
    }

    if ($null -eq $extractedIcon) {
        return $null
    }

    $ms = New-Object System.IO.MemoryStream
    $extractedIcon.ToBitmap().Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $ms.Position = 0
    $bi = New-Object System.Windows.Media.Imaging.BitmapImage
    $bi.BeginInit()
    $bi.StreamSource = $ms
    $bi.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bi.EndInit()
    $bi.Freeze()
    $ms.Close()
    return $bi
}

function Open-Menu {
    $Xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="QuickLaunchWindow"
        WindowStyle="None" AllowsTransparency="True" Background="Transparent"
        Topmost="True" ShowInTaskbar="False" ResizeMode="NoResize" Width="300" SizeToContent="Height">
    <Window.Resources>
        <Style TargetType="Button">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="{TemplateBinding VerticalContentAlignment}"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
    <Border CornerRadius="8" Background="$BgColor" BorderBrush="$BorderColor" BorderThickness="1" Margin="10">
        <Border.Effect>
            <DropShadowEffect Color="Black" Direction="270" ShadowDepth="2" Opacity="0.4" BlurRadius="8"/>
        </Border.Effect>
        <ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="600">
            <StackPanel Name="ShortcutList" Margin="5" />
        </ScrollViewer>
    </Border>
</Window>
"@

    $reader = (New-Object System.Xml.XmlNodeReader([xml]$Xaml))
    $Window = [System.Windows.Markup.XamlReader]::Load($reader)
    $ShortcutList = $Window.FindName("ShortcutList")

    $Window.Add_Deactivated({
        $Window.DialogResult = $false
    })

    if (Test-Path $Settings.ShortcutFolder) {
        $shortcuts = Get-ChildItem -Path $Settings.ShortcutFolder -Filter "*.lnk" | Sort-Object Name
        foreach ($s in $shortcuts) {
            $btn = New-Object System.Windows.Controls.Button
            $btn.Background = [System.Windows.Media.Brushes]::Transparent
            $btn.BorderThickness = "0"
            $btn.HorizontalContentAlignment = [System.Windows.HorizontalAlignment]::Left
            $btn.Padding = "10,8"
            
            $stack = New-Object System.Windows.Controls.StackPanel
            $stack.Orientation = [System.Windows.Controls.Orientation]::Horizontal
            
            $img = New-Object System.Windows.Controls.Image
            $img.Source = Get-ImageSourceFromIcon -IconPath $s.FullName -ShortcutPath $s.FullName
            $img.Width = $Settings.IconSize
            $img.Height = $Settings.IconSize
            $img.Margin = "0,0,12,0"

            $txt = New-Object System.Windows.Controls.TextBlock
            $txt.Text = [System.IO.Path]::GetFileNameWithoutExtension($s.Name)
            $txt.VerticalAlignment = [System.Windows.VerticalAlignment]::Center
            $txt.Foreground = (New-Object System.Windows.Media.BrushConverter).ConvertFromString($FgColor)
            $txt.FontSize = 14
            $txt.FontFamily = New-Object System.Windows.Media.FontFamily("Segoe UI")

            $stack.Children.Add($img) | Out-Null
            if ($Settings.ShowLabels) { $stack.Children.Add($txt) | Out-Null }
            
            $btn.Content = $stack
            
            $btn.Add_MouseEnter({
                param($ctrl, $e)
                $ctrl.Background = (New-Object System.Windows.Media.BrushConverter).ConvertFromString($script:HoverColor)
            })
            $btn.Add_MouseLeave({
                param($ctrl, $e)
                $ctrl.Background = [System.Windows.Media.Brushes]::Transparent
            })

            $btn.Add_Click({
                param($ctrl, $e)
                $itemPath = $ctrl.Tag
                try {
                    Start-Process $itemPath -ErrorAction SilentlyContinue
                    Write-Log "Launched: $itemPath"
                } catch {
                    Write-Log "Failed to launch $itemPath : $_"
                }
                $Window.DialogResult = $true
            })
            $btn.Tag = $s.FullName
            
            $ShortcutList.Children.Add($btn) | Out-Null
        }
    }
    
    if ($ShortcutList.Children.Count -eq 0) {
        $txt = New-Object System.Windows.Controls.TextBlock
        $txt.Text = "No shortcuts found."
        $txt.Foreground = (New-Object System.Windows.Media.BrushConverter).ConvertFromString($FgColor)
        $txt.Margin = "10"
        $ShortcutList.Children.Add($txt) | Out-Null
    }

    $Window.Add_Loaded({
        $graphics = [System.Drawing.Graphics]::FromHwnd([IntPtr]::Zero)
        $dpiX = $graphics.DpiX
        $dpiY = $graphics.DpiY
        $graphics.Dispose()
        
        $scaleX = $dpiX / 96.0
        $scaleY = $dpiY / 96.0

        $mousePos = [System.Windows.Forms.Cursor]::Position
        $screen = [System.Windows.Forms.Screen]::FromPoint($mousePos)
        
        $logicalRight = $screen.WorkingArea.Right / $scaleX
        $logicalBottom = $screen.WorkingArea.Bottom / $scaleY
        $logicalTop = $screen.WorkingArea.Top / $scaleY
        
        $logicalMouseX = $mousePos.X / $scaleX
        
        $Window.Left = $logicalMouseX - ($Window.ActualWidth / 2)
        
        if (($Window.Left + $Window.ActualWidth) -gt $logicalRight) {
            $Window.Left = $logicalRight - $Window.ActualWidth
        }
        
        $Window.Top = $logicalBottom - $Window.ActualHeight + 5
        
        if ($Window.Top -lt $logicalTop) {
            $Window.Top = $logicalTop
        }
    })

    $Window.ShowDialog() | Out-Null
}

$NotifyIcon = New-Object System.Windows.Forms.NotifyIcon
$DefaultIconPath = Join-Path $ScriptDir "icons\icon.ico"

if ($Settings.TrayIconPath -and (Test-Path $Settings.TrayIconPath)) {
    $NotifyIcon.Icon = New-Object System.Drawing.Icon($Settings.TrayIconPath)
} elseif (Test-Path $DefaultIconPath) {
    $NotifyIcon.Icon = New-Object System.Drawing.Icon($DefaultIconPath)
} else {
    $NotifyIcon.Icon = [System.Drawing.SystemIcons]::Application
}
$NotifyIcon.Text = "Quick Launch"
$NotifyIcon.Visible = $true
Write-Log "QuickLaunch started"

$ContextMenu = New-Object System.Windows.Forms.ContextMenu

$ConfigItem = New-Object System.Windows.Forms.MenuItem("Config")
$ConfigItem.Add_Click({
    if (Test-Path $Settings.ShortcutFolder) {
        Start-Process "explorer.exe" $Settings.ShortcutFolder
    } else {
        [System.Windows.Forms.MessageBox]::Show("Shortcut folder does not exist: $($Settings.ShortcutFolder)", "Config", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
    }
})
$ContextMenu.MenuItems.Add($ConfigItem) | Out-Null

$EditSettingsItem = New-Object System.Windows.Forms.MenuItem("Edit Settings")
$EditSettingsItem.Add_Click({
    try {
        Start-Process $SettingsFile
        Write-Log "Opened settings file: $SettingsFile"
    } catch {
        Write-Log "Failed to open settings file: $_"
    }
})
$ContextMenu.MenuItems.Add($EditSettingsItem) | Out-Null

$ContextMenu.MenuItems.Add((New-Object System.Windows.Forms.MenuItem("-"))) | Out-Null

$QuitItem = New-Object System.Windows.Forms.MenuItem("Quit")
$QuitItem.Add_Click({
    Write-Log "QuickLaunch exiting"
    $NotifyIcon.Visible = $false
    [Environment]::Exit(0)
})
$ContextMenu.MenuItems.Add($QuitItem) | Out-Null

$NotifyIcon.ContextMenu = $ContextMenu

$NotifyIcon.Add_Click({
    try {
        Open-Menu
    } catch {
        Write-Log "Error in Open-Menu: $($_.Exception.ToString())"
        [System.Windows.Forms.MessageBox]::Show($_.Exception.ToString(), "Error in Open-Menu", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})

[System.Windows.Forms.Application]::Run()
