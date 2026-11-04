[CmdletBinding()]
param(
    [ValidateSet('List','Scan','Options')][string]$Action = 'List',
    [ValidateSet(50,75,150,300,600)][int]$Dpi = 150,
    [ValidateSet('Color','Gray')][string]$Mode = 'Color',
    [string]$OutputFile
)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$binDir = Join-Path $taskRoot 'runtime\ucrt64\bin'
$scanner = Join-Path $binDir 'scanimage.exe'
$configDir = Join-Path $taskRoot 'config'
# GT68xx on Windows recognizes an absolute firmware path only if it starts
# with a backslash. The Windows extended path prefix satisfies that check
# while retaining the explicit drive and avoiding dependence on the cwd.
$firmware = '\\?\' + (Join-Path $taskRoot 'firmware\SBfw.usb')
if(-not (Test-Path -LiteralPath (Join-Path $taskRoot 'firmware\SBfw.usb'))) { throw 'Firmware file is missing.' }
$oldPath = $env:PATH
$oldConfig = $env:SANE_CONFIG_DIR
$oldDebug = $env:SANE_DEBUG_GT68XX
try {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    $configLines = @('# Mustek 1200 UB Plus', 'usb 0x05d8 0x4002', 'override "mustek-scanexpress-1200-ub-plus"', ('firmware "' + $firmware + '"'))
    [IO.File]::WriteAllLines((Join-Path $configDir 'gt68xx.conf'), $configLines, (New-Object Text.UTF8Encoding $false))
    $env:PATH = $binDir + ';' + $oldPath
    $env:SANE_CONFIG_DIR = $configDir
    # Keep optional debug logging off during normal scans.
    $env:SANE_DEBUG_GT68XX = $null
    if(-not (Test-Path -LiteralPath $scanner)) { throw 'Runtime not prepared.' }
    $deviceList = @(& $scanner '--formatted-device-list=%d%n')
    if($LASTEXITCODE -ne 0) { throw ('Device enumeration failed: ' + $LASTEXITCODE) }
    $devices = @($deviceList | Where-Object { $_ -and $_.Trim() })
    if($devices.Count -eq 0) {
        throw 'Scanner not accessible. Install WinUSB for USB VID 05D8 PID 4002 using Zadig, then reconnect it and retry.'
    }
    if($devices.Count -ne 1) { throw 'More than one matching scanner found. Connect only one for this test.' }
    $device = $devices[0].Trim()
    if($Action -eq 'List') {
        Write-Output ('Mustek 1200 UB Plus: ' + $device)
        exit 0
    }
    if($Action -eq 'Options') {
        & $scanner '--device-name' $device '--help'
        exit $LASTEXITCODE
    }
    $scansDir = Join-Path $taskRoot 'scans'
    New-Item -ItemType Directory -Path $scansDir -Force | Out-Null
    if(-not $OutputFile) { $OutputFile = Join-Path $scansDir ('scan-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.png') }
    $OutputFile = [IO.Path]::GetFullPath($OutputFile)
    if(Test-Path -LiteralPath $OutputFile) { throw ('Output already exists: ' + $OutputFile) }
    & $scanner '--device-name' $device '--mode' $Mode '--resolution' $Dpi '--depth' '8' '--format=png' ('--output-file=' + $OutputFile) '--progress'
    if($LASTEXITCODE -ne 0) { throw ('Scan failed: ' + $LASTEXITCODE + '. Output may be incomplete: ' + $OutputFile) }
    if(-not (Test-Path -LiteralPath $OutputFile)) { throw 'Scan did not create a file.' }
    Add-Type -AssemblyName System.Drawing
    $scanImage = [Drawing.Image]::FromFile($OutputFile)
    try {
        if($scanImage.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) { throw 'Output is not a valid PNG.' }
        $imageSize = '{0} x {1}' -f $scanImage.Width, $scanImage.Height
    } finally { $scanImage.Dispose() }
    Write-Output ('Saved: ' + $OutputFile + ' (' + $imageSize + ')')
} catch {
    Write-Error $_
    exit 1
} finally {
    $env:PATH = $oldPath
    $env:SANE_CONFIG_DIR = $oldConfig
    $env:SANE_DEBUG_GT68XX = $oldDebug
}
