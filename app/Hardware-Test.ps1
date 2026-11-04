$ErrorActionPreference = 'Stop'
if(Get-Process scanimage -ErrorAction SilentlyContinue) { throw 'Scanner is currently busy.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'PdfSharp.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'MustekStudio.exe')) | Out-Null
$output = Join-Path $projectRoot ('app-tests\hardware-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.png')
$engine = New-Object MustekStudio.ScannerEngine -ArgumentList $projectRoot,(Join-Path $projectRoot 'app-tests\hardware-logs')
$engine.Scan($output,150,'Color',$false,$null)
Write-Output ('Hardware scan passed: ' + $output)
