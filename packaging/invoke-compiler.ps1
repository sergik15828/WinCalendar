param([Parameter(Mandatory=$true)][string]$Compiler, [Parameter(Mandatory=$true)][string[]]$CompilerArguments, [Parameter(Mandatory=$true)][string]$LogPrefix)
$ErrorActionPreference = 'Stop'
$quoted = @($CompilerArguments | ForEach-Object { '"' + $_ + '"' })
$stdout = $LogPrefix + '.out.txt'
$stderr = $LogPrefix + '.err.txt'
# Start-Process also works from WSL when native invocation cannot resolve PATHEXT.
$process = Start-Process $Compiler -ArgumentList $quoted -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
Get-Content $stdout -Encoding UTF8
Get-Content $stderr -Encoding UTF8
if ($process.ExitCode -ne 0) { throw ('Installer compiler failed with exit code ' + $process.ExitCode + '. See ' + $LogPrefix) }
