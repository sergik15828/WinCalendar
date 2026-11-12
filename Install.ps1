param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$sourceDir = Join-Path $root 'dist'
$sourceExe = Join-Path $sourceDir 'WinDate.exe'
if (!(Test-Path $sourceExe)) { & (Join-Path $root 'build.ps1') }
$installDir = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\WinDate'
$installedExe = Join-Path $installDir 'WinDate.exe'
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
$changed = !(Test-Path $installedExe) -or (Get-FileHash $sourceExe).Hash -ne (Get-FileHash $installedExe).Hash
if ($changed) {
    Get-Process -Name WinDate -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $sourceExe -or $_.Path -eq $installedExe } | ForEach-Object {
        Stop-Process -Id $_.Id
        [void]$_.WaitForExit(3000)
    }
    Copy-Item $sourceExe $installedExe -Force
}
Copy-Item (Join-Path $sourceDir 'WinDate.exe.config') ($installedExe + '.config') -Force
Copy-Item (Join-Path $root 'Settings.cmd') (Join-Path $installDir 'Settings.cmd') -Force
# Keep an existing startup preference when moving out of the development folder.
$run = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run', $true)
if ($null -ne $run) {
    try {
        $previous = $run.GetValue('WinCalendar') -as [string]
        $legacyExe = Join-Path $sourceDir 'WinCalendar.exe'
        if ($previous -eq ('"' + $sourceExe + '" --background') -or $previous -eq ('"' + $legacyExe + '" --background') -or $previous -eq ('"' + $installedExe + '" --background')) {
            $explorer = Join-Path ([Environment]::GetFolderPath('Windows')) 'explorer.exe'
            $run.SetValue('WinCalendar', '"' + $explorer + '" "' + $installedExe + '"')
        }
    } finally { $run.Dispose() }
}
if (!$NoLaunch) {
    # Use the real desktop broker. Shell.Application.ShellExecute creates a child
    # of the temporary PowerShell process and caused the taskbar icon to disappear.
    Start-Process -FilePath (Join-Path $env:WINDIR 'explorer.exe') -ArgumentList ('"' + $installedExe + '"')
}
Write-Output ('Installed: ' + $installedExe)
