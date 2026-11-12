param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path $compiler)) { throw 'The Windows .NET Framework C# compiler was not found.' }
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xaml.dll', 'System.Net.Http.dll', 'System.Web.dll', 'System.Web.Extensions.dll', 'System.Security.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll', 'PresentationCore.dll', 'PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_)) }
$sources = @(Get-ChildItem (Join-Path $root 'src\*.cs') | ForEach-Object { $_.FullName })
$common = @('/nologo', '/utf8output', '/codepage:65001', '/optimize+', '/platform:x64', '/warn:4', '/warnaserror+', '/target:winexe', ('/win32manifest:' + (Join-Path $root 'app.manifest')))
function Invoke-Compiler([string[]]$CompilerArguments) {
    # Start-Process also works when launched from WSL, where PATHEXT may be absent.
    $quoted = @($CompilerArguments | ForEach-Object { '"' + $_ + '"' })
    $stdout = Join-Path $output 'compiler-output.txt'
    $stderr = Join-Path $output 'compiler-errors.txt'
    $compileProcess = Start-Process -FilePath $compiler -ArgumentList $quoted -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Get-Content $stdout -Encoding UTF8
    Get-Content $stderr -Encoding UTF8
    if ($compileProcess.ExitCode -ne 0) { throw 'C# compilation failed.' }
    Remove-Item $stdout, $stderr -Force
}
$bootstrap = Join-Path $output 'WinCalendar.Build.exe'
Invoke-Compiler ($common + $references + @('/out:' + $bootstrap) + $sources)
Copy-Item (Join-Path $root 'WinCalendar.exe.config') ($bootstrap + '.config') -Force
$icon = Join-Path $output 'WinCalendar.ico'
$process = Start-Process -FilePath $bootstrap -ArgumentList @('--export-icon', ('"' + $icon + '"')) -Wait -PassThru
if ($process.ExitCode -ne 0) { throw 'Calendar icon generation failed.' }
$exe = Join-Path $output 'WinDate.exe'
Invoke-Compiler ($common + $references + @('/win32icon:' + $icon) + @('/out:' + $exe) + $sources)
Copy-Item (Join-Path $root 'WinCalendar.exe.config') ($exe + '.config') -Force
foreach ($legacyName in @('WinCalendar.exe', 'WinCalendar.exe.config')) {
    $legacyPath = Join-Path $output $legacyName
    if (Test-Path $legacyPath) { Remove-Item $legacyPath -Force }
}
Remove-Item $bootstrap, ($bootstrap + '.config') -Force
Write-Output ('Built: ' + $exe)
if ($Test) {
    $artifacts = Join-Path $root 'artifacts'
    New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
    $process = Start-Process -FilePath $exe -ArgumentList @('--self-test', ('"' + $artifacts + '"')) -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw ('Self-test failed. See ' + (Join-Path $artifacts 'self-test.txt')) }
    Get-Content (Join-Path $artifacts 'self-test.txt') -Encoding UTF8
}
