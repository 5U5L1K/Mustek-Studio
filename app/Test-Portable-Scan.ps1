param([Parameter(Mandatory=$true)][string]$PortableExe)
$ErrorActionPreference = 'Stop'
if(Get-Process scanimage -ErrorAction SilentlyContinue) { throw 'Scanner is busy.' }
$assembly = [Reflection.Assembly]::LoadFrom($PortableExe)
$bootstrap = $assembly.GetType('MustekStudio.PortablePackage', $true)
$bootstrap.GetMethod('Initialize').Invoke($null, @()) | Out-Null
$engineRoot = [string]$bootstrap.GetField('EngineRoot').GetValue($null)
$dataRoot = [string]$bootstrap.GetField('DataRoot').GetValue($null)
$output = Join-Path $dataRoot ('checks\hardware-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.png')
$constructor = $assembly.GetType('MustekStudio.ScannerEngine').GetConstructor([type[]]@([string],[string]))
$engine = $constructor.Invoke([object[]]@([string]$engineRoot,[string](Join-Path $dataRoot 'checks\hardware-logs')))
$engine.Scan($output,75,'Color',$false,$null)
Write-Output ('Portable hardware scan: ' + $output)
