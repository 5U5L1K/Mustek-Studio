$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /langversion:5 `
    ('/out:' + (Join-Path $PSScriptRoot 'ScannerConnection.exe')) `
    ('/win32manifest:' + (Join-Path $projectRoot 'app\app.manifest')) `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll `
    (Join-Path $PSScriptRoot 'ScannerConnection.cs')
if($LASTEXITCODE -ne 0) { throw 'Scanner connection assistant build failed.' }
$zadig = Join-Path $projectRoot 'downloads\zadig-2.9.exe'
if((Get-FileHash -LiteralPath $zadig).Hash.ToLowerInvariant() -ne '4ecaa95df3da3621486a043aef8b3050b8bafe7c901402871e816229ef82039b') { throw 'Unexpected Zadig binary.' }
$signature = Get-AuthenticodeSignature -LiteralPath $zadig
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Akeo Consulting') { throw 'Invalid Zadig signature.' }
& (Join-Path $projectRoot 'tools\InnoSetup7\ISCC.exe') (Join-Path $PSScriptRoot 'MustekStudio.iss')
if($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
$output = Join-Path $projectRoot 'release\MustekStudio-Setup.exe'
$hash = (Get-FileHash -LiteralPath $output).Hash.ToLowerInvariant()
($hash + '  MustekStudio-Setup.exe') | Set-Content -LiteralPath (Join-Path $projectRoot 'release\Setup-SHA256.txt') -Encoding ASCII
Write-Output ('Setup: ' + $output)
Write-Output ('Size: ' + (Get-Item -LiteralPath $output).Length + ' bytes')
