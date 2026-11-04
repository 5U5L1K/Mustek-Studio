$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$python = (Get-Command python.exe -ErrorAction Stop).Source
& $python (Join-Path $PSScriptRoot 'prepare_portable.py')
if($LASTEXITCODE -ne 0) { throw 'Portable payload preparation failed.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$pdf = Join-Path $PSScriptRoot 'vendor\PDFsharp-1.50.5147\lib\net20\PdfSharp.dll'
$release = Join-Path $projectRoot 'release'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$output = Join-Path $release 'MustekStudio-Portable.exe'
$assets = Join-Path $PSScriptRoot 'portable-build'
$icon = Join-Path $PSScriptRoot 'assets\mustek-scanner-app-icon.ico'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /langversion:5 /define:PORTABLE `
    ('/out:' + $output) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) `
    ('/win32icon:' + $icon) ('/resource:' + $icon + ',MustekStudio.AppIcon') `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll `
    /reference:System.IO.Compression.dll `
    ('/reference:' + $pdf) ('/resource:' + $pdf + ',Portable.PdfSharp.dll') `
    ('/resource:' + (Join-Path $assets 'payload.zip') + ',Portable.payload.zip') `
    ('/resource:' + (Join-Path $assets 'manifest.tsv') + ',Portable.manifest.tsv') `
    ('/resource:' + (Join-Path $assets 'version.txt') + ',Portable.version.txt') `
    ('/resource:' + (Join-Path $assets 'defaults.xml') + ',Portable.defaults.xml') `
    (Join-Path $PSScriptRoot 'ScannerCore.cs') (Join-Path $PSScriptRoot 'MustekStudio.cs') (Join-Path $PSScriptRoot 'Appearance.cs') `
    (Join-Path $PSScriptRoot 'PortablePackage.cs') (Join-Path $PSScriptRoot 'PortableChecks.cs') (Join-Path $PSScriptRoot 'PortableAssemblyInfo.cs')
if($LASTEXITCODE -ne 0) { throw 'Portable application build failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PORTABLE-README.txt') -Destination (Join-Path $release 'README.txt') -Force
$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
($hash + '  MustekStudio-Portable.exe') | Set-Content -LiteralPath (Join-Path $release 'SHA256.txt') -Encoding ASCII
Write-Output ('Portable EXE: ' + $output)
Write-Output ('Size: ' + (Get-Item -LiteralPath $output).Length + ' bytes')
Write-Output ('SHA256: ' + $hash)
