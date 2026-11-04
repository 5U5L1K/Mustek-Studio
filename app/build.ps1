$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$pdf = Join-Path $PSScriptRoot 'vendor\PDFsharp-1.50.5147\lib\net20\PdfSharp.dll'
$output = Join-Path $PSScriptRoot 'MustekStudio.exe'
$icon = Join-Path $PSScriptRoot 'assets\mustek-scanner-app-icon.ico'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /langversion:5 `
    ('/out:' + $output) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) `
    ('/win32icon:' + $icon) ('/resource:' + $icon + ',MustekStudio.AppIcon') `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll `
    ('/reference:' + $pdf) (Join-Path $PSScriptRoot 'ScannerCore.cs') (Join-Path $PSScriptRoot 'MustekStudio.cs') (Join-Path $PSScriptRoot 'Appearance.cs')
if($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$pdfDestination = Join-Path $PSScriptRoot 'PdfSharp.dll'
if(-not (Test-Path -LiteralPath $pdfDestination) -or (Get-FileHash -LiteralPath $pdf).Hash -ne (Get-FileHash -LiteralPath $pdfDestination).Hash) {
    Copy-Item -LiteralPath $pdf -Destination $pdfDestination -Force
}
Write-Output ('Built: ' + $output)
