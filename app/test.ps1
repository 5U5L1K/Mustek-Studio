param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if(-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot ('app-tests\run-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe = Join-Path $PSScriptRoot 'CoreTests.exe'
& $compiler /nologo /target:exe /platform:x64 /optimize+ /codepage:65001 /langversion:5 /main:MustekStudio.CoreTests ('/out:' + $exe) `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Xml.dll /reference:System.Windows.Forms.dll `
    ('/reference:' + (Join-Path $PSScriptRoot 'PdfSharp.dll')) (Join-Path $PSScriptRoot 'ScannerCore.cs') (Join-Path $PSScriptRoot 'MustekStudio.cs') (Join-Path $PSScriptRoot 'Appearance.cs') (Join-Path $PSScriptRoot 'CoreTests.cs')
if($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& $exe $projectRoot $OutputDirectory (Join-Path $projectRoot 'scans\scan-20261104-212251-619.png')
if($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
