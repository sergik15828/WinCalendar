param([string]$Compiler, [switch]$SkipBuild, [switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (!$SkipBuild) { & (Join-Path $root 'build.ps1') }
if (!$Compiler) { $Compiler = Join-Path $root 'artifacts\tools\InnoSetup7\ISCC.exe' }
if (!(Test-Path $Compiler)) { throw 'Inno Setup 7 compiler not found. Run packaging\bootstrap-inno.ps1 or pass -Compiler.' }
$exe = Join-Path $root 'dist\WinDate.exe'
$version = ([version][Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion).ToString(3)
$payload = Join-Path $root 'artifacts\installer-payload'
New-Item -ItemType Directory -Force -Path $payload | Out-Null
# Deliberate allowlist: no user profile, credentials, caches, logs or build tools.
Copy-Item $exe, ($exe + '.config') $payload -Force
foreach ($name in @('Settings.cmd', 'EnableWeatherPrototype.cmd', 'DisableWeatherPrototype.cmd', 'GOOGLE_SETUP.md')) {
    Copy-Item (Join-Path $root $name) $payload -Force
}
$guide = Get-Content (Join-Path $root 'packaging\GETTING_STARTED.txt') -Raw -Encoding UTF8
[IO.File]::WriteAllText((Join-Path $payload 'GETTING_STARTED.txt'), $guide, (New-Object Text.UTF8Encoding($true)))
$expected = @('WinDate.exe', 'WinDate.exe.config', 'Settings.cmd', 'EnableWeatherPrototype.cmd', 'DisableWeatherPrototype.cmd', 'GOOGLE_SETUP.md', 'GETTING_STARTED.txt')
if (@(Get-ChildItem $payload -File | Where-Object { $_.Name -notin $expected }).Count) { throw 'Unexpected files in installer payload.' }
& (Join-Path $root 'packaging\invoke-compiler.ps1') -Compiler $Compiler -CompilerArguments @(('/DAppVersion=' + $version), (Join-Path $root 'packaging\WinCalendar.iss')) -LogPrefix (Join-Path $root 'artifacts\installer-compiler')
$releaseDir = Join-Path $root 'dist\releases'
$installer = Join-Path $releaseDir ('WinCalendar-' + $version + '-Setup-x64.exe')
$digest = (Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $releaseDir 'SHA256SUMS.txt'), ($digest + '  ' + [IO.Path]::GetFileName($installer) + "`n"), (New-Object Text.UTF8Encoding($false)))
if ($Test) {
    & (Join-Path $root 'packaging\test-installer.ps1') -Compiler $Compiler -Version $version
}
Write-Output ('Installer: ' + $installer)
Write-Output ('SHA256: ' + $digest)
