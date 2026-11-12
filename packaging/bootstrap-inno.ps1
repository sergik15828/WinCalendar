param([string]$Destination = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\tools\InnoSetup7'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $Destination 'ISCC.exe'
if (Test-Path $compiler) { Write-Output $compiler; exit 0 }
$download = Join-Path (Split-Path $Destination -Parent) 'innosetup-7.1.1-x64.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $download -Parent) | Out-Null
Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe' -OutFile $download
$expected = '8a5a6711c3766a6e5fb74f280ba0e3551e719ce94538d901fdbd1f8f9920b3f0'
if ((Get-FileHash $download -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Inno Setup download checksum mismatch.' }
$signature = Get-AuthenticodeSignature $download
if ($signature.Status -ne 'Valid') { throw ('Inno Setup signature is not valid: ' + $signature.Status) }
Write-Output ('Verified compiler installer: ' + $signature.SignerCertificate.Subject)
$arguments = @('/CURRENTUSER', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/NOICONS', '/TASKS=""', ('/DIR="' + $Destination + '"'))
$installation = Start-Process $download -ArgumentList $arguments -Wait -PassThru
if ($installation.ExitCode -ne 0 -or !(Test-Path $compiler)) { throw 'Inno Setup installation failed.' }
Write-Output $compiler
