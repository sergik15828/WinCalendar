param([Parameter(Mandatory=$true)][string]$Compiler, [Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $root ('artifacts\installer-qa-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$results = New-Object 'System.Collections.Generic.List[string]'
function Confirm([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
    $results.Add('PASS ' + $Message)
    Write-Output ('PASS ' + $Message)
}
function Run-Setup([string]$File, [string[]]$Arguments) {
    $process = Start-Process $File -ArgumentList $Arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw ('Installer process failed with exit code ' + $process.ExitCode) }
}
function Read-RunValue([string]$Subkey) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Subkey)
    try { if ($key) { return $key.GetValue('WinCalendar', $null) }; return $null }
    finally { if ($key) { $key.Dispose() } }
}
$actualRunKey = 'Software\Microsoft\Windows\CurrentVersion\Run'
$actualStartupBefore = Read-RunValue $actualRunKey
$applicationDir = Join-Path $testRoot 'application'
New-Item -ItemType Directory -Path $applicationDir | Out-Null
Run-Setup (Join-Path $root 'dist\WinDate.exe') @('--self-test', ('"' + $applicationDir + '"'))
$applicationChecks = @(Get-Content (Join-Path $applicationDir 'self-test.txt') -Encoding UTF8 | Where-Object { $_ -like 'PASS *' }).Count
Confirm ($applicationChecks -gt 0) ('Application self-test: ' + $applicationChecks + ' successful checks')

# A compile-time variant redirects every shortcut and startup entry to a
# synthetic profile. It never starts or stops the user's running calendar.
& (Join-Path $PSScriptRoot 'invoke-compiler.ps1') -Compiler $Compiler -CompilerArguments @('/DInstallerSmokeTest', ('/DAppVersion=' + $Version), ('/O' + $testRoot), '/FWinCalendar-Installer-Smoke', (Join-Path $root 'packaging\WinCalendar.iss')) -LogPrefix (Join-Path $testRoot 'smoke-compiler')
$setup = Join-Path $testRoot 'WinCalendar-Installer-Smoke.exe'
$installDir = Join-Path $testRoot 'Installed App'
$stateDir = Join-Path $installDir 'SandboxProfile\WinCalendar'
$menuDir = Join-Path $installDir 'SandboxProfile\Programs\WinCalendar'
$smokeRunKey = 'Software\WinCalendar\InstallerSmoke\Run'
$common = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/LANG=english', ('/DIR="' + $installDir + '"'))
Run-Setup $setup ($common + @('/TASKS="startup,desktopicon"', ('/LOG="' + (Join-Path $testRoot 'install.log') + '"')))
$installedExe = Join-Path $installDir 'WinDate.exe'
$expectedHash = (Get-FileHash (Join-Path $root 'dist\WinDate.exe')).Hash
Confirm ((Get-FileHash $installedExe).Hash -eq $expectedHash) 'Fresh installation has the exact release application binary'
Confirm ([Diagnostics.FileVersionInfo]::GetVersionInfo($installedExe).FileVersion -eq ($Version + '.0')) 'Installed file version matches the release'
Confirm (Test-Path ($installedExe + '.config')) '.NET runtime configuration is installed'
$shell = New-Object -ComObject WScript.Shell
try {
    $mainLink = $shell.CreateShortcut((Join-Path $menuDir 'WinCalendar.lnk'))
    $settingsLink = $shell.CreateShortcut((Join-Path $menuDir 'WinCalendar Settings.lnk'))
    $startupLink = $shell.CreateShortcut((Join-Path $stateDir 'Startup.lnk'))
    Confirm ($mainLink.TargetPath -eq $installedExe -and $settingsLink.Arguments -eq '--menu') 'Start menu shortcuts open the app and its settings'
    Confirm ($startupLink.TargetPath -eq $installedExe -and $startupLink.Arguments -eq '--background') 'Startup shortcut launches the hidden background mode'
} finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
$run = Read-RunValue $smokeRunKey
Confirm ($run -and $run.Contains('explorer.exe') -and $run.Contains((Join-Path $stateDir 'Startup.lnk'))) 'Startup uses the Explorer broker and the background shortcut'
Confirm (Test-Path (Join-Path $installDir 'SandboxProfile\Desktop\WinCalendar.lnk')) 'Optional desktop shortcut is installed'
Confirm ((Test-Path (Join-Path $installDir 'GETTING_STARTED.txt')) -and (Test-Path (Join-Path $installDir 'GOOGLE_SETUP.md'))) 'First-run and Google setup instructions are included'

$marker = Join-Path $stateDir 'Preferences.test-marker'
[IO.File]::WriteAllText($marker, 'synthetic profile: preserve during upgrades and uninstall')
Run-Setup $setup ($common + @('/TASKS=""', ('/LOG="' + (Join-Path $testRoot 'upgrade.log') + '"')))
Confirm ($null -eq (Read-RunValue $smokeRunKey)) 'An upgrade can disable the app startup entry'
Confirm ((Get-FileHash $installedExe).Hash -eq $expectedHash -and (Test-Path $marker)) 'An upgrade preserves the existing profile'
Run-Setup $setup ($common + @('/TASKS="startup"'))
Confirm ($null -ne (Read-RunValue $smokeRunKey)) 'Startup can be enabled again during upgrade'
$uninstaller = @(Get-ChildItem $installDir -Filter 'unins*.exe')[0].FullName
Run-Setup $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/LOG="' + (Join-Path $testRoot 'uninstall.log') + '"'))
Confirm (!(Test-Path $installedExe) -and !(Test-Path (Join-Path $menuDir 'WinCalendar.lnk'))) 'Uninstall removes application files and shortcuts'
Confirm ($null -eq (Read-RunValue $smokeRunKey)) 'Uninstall removes its startup entry'
Confirm (Test-Path $marker) 'Uninstall preserves user profile data'
Confirm ((Read-RunValue $actualRunKey) -eq $actualStartupBefore) 'Installer tests leave the real user startup preference unchanged'
$results | Set-Content (Join-Path $testRoot 'installer-validation.txt') -Encoding UTF8
Write-Output ('Validation: ' + $testRoot)
