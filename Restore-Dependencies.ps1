param(
    [string]$InstalledExe = (Join-Path $env:LOCALAPPDATA 'Programs\MustekStudio\MustekStudio.exe')
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $InstalledExe -PathType Leaf)) {
    throw 'Install release\MustekStudio-Setup.exe first, or pass -InstalledExe with the installed application path.'
}
Add-Type -AssemblyName System.IO.Compression
$installedPath = (Resolve-Path -LiteralPath $InstalledExe).Path
$installedDirectory = Split-Path -Parent $installedPath
$zadigPath = Join-Path $installedDirectory 'tools\zadig-2.9.exe'
if (-not (Test-Path -LiteralPath $zadigPath)) { throw 'The installed setup tools\zadig-2.9.exe is missing.' }
if ((Get-FileHash -LiteralPath $zadigPath).Hash.ToLowerInvariant() -ne '4ecaa95df3da3621486a043aef8b3050b8bafe7c901402871e816229ef82039b') {
    throw 'Unexpected Zadig binary.'
}
$application = [System.Reflection.Assembly]::LoadFile($installedPath)
$rootPrefix = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

function Read-Resource([string]$Name) {
    $stream = $application.GetManifestResourceStream('Portable.' + $Name)
    if ($null -eq $stream) { throw ('Not a portable Mustek Studio application: missing ' + $Name) }
    $memory = New-Object IO.MemoryStream
    try { $stream.CopyTo($memory); return ,$memory.ToArray() }
    finally { $stream.Dispose(); $memory.Dispose() }
}
function Get-BytesHash([byte[]]$Bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Write-Dependency([string]$Relative, [byte[]]$Bytes) {
    $target = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $Relative))
    if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid dependency destination.' }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::WriteAllBytes($target, $Bytes)
}

$payload = Read-Resource 'payload.zip'
$version = [Text.Encoding]::UTF8.GetString((Read-Resource 'version.txt')).Trim()
if ((Get-BytesHash $payload) -ne $version) { throw 'Embedded payload checksum mismatch.' }
$manifest = [Text.Encoding]::UTF8.GetString((Read-Resource 'manifest.tsv'))
$memory = [IO.MemoryStream]::new($payload, $false)
$archive = [IO.Compression.ZipArchive]::new($memory, [IO.Compression.ZipArchiveMode]::Read)
$seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$audit = $null
try {
    foreach ($line in ($manifest -split "`n")) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $fields = $line.TrimEnd("`r") -split "`t"
        if ($fields.Length -ne 3 -or -not $seen.Add($fields[2])) { throw 'Invalid embedded manifest.' }
        $name = $fields[2]
        $entry = $archive.GetEntry($name)
        if ($null -eq $entry -or $entry.Length -ne [long]$fields[1]) { throw ('Missing package entry: ' + $name) }
        $stream = $entry.Open(); $content = New-Object IO.MemoryStream
        try { $stream.CopyTo($content); $bytes = $content.ToArray() }
        finally { $stream.Dispose(); $content.Dispose() }
        if ((Get-BytesHash $bytes) -ne $fields[0]) { throw ('Package checksum mismatch: ' + $name) }
        $destination = $null
        if ($name.StartsWith('runtime/ucrt64/bin/')) { $destination = $name }
        elseif ($name -eq 'sane/gt68xx/SBfw.usb') { $destination = 'firmware/SBfw.usb' }
        elseif ($name.StartsWith('licenses/sane/')) { $destination = 'runtime/ucrt64/share/doc/sane-backends/' + $name.Substring(14) }
        elseif ($name.StartsWith('licenses/') -and $name -ne 'licenses/PDFsharp-LICENSE.txt') {
            $destination = 'runtime/ucrt64/share/licenses/' + $name.Substring(9)
        }
        elseif ($name -eq 'source/backends-1.4.0.tar.bz2') { $destination = 'downloads/backends-1.4.0.tar.bz2' }
        elseif ($name -eq 'portable-data-directory-fix.json') { $audit = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json }
        if ($destination) { Write-Dependency $destination $bytes }
    }
    if ($seen.Count -ne $archive.Entries.Count) { throw 'Package manifest does not cover every file.' }
} finally { $archive.Dispose(); $memory.Dispose() }

# prepare_portable.py expects the original working DLL and applies the relative
# data-directory patch itself. Reverse only that verified packaging change.
if ($null -eq $audit) { throw 'Portable data-directory audit is missing.' }
$dllPath = Join-Path $PSScriptRoot 'runtime\ucrt64\bin\libsane-1.dll'
$dll = [IO.File]::ReadAllBytes($dllPath)
if ((Get-BytesHash $dll) -ne $audit.after_sha256) { throw 'Unexpected portable scanner DLL.' }
$original = [Text.Encoding]::ASCII.GetBytes($audit.old_data_directory)
$offset = [int]$audit.offset
if ($offset -lt 0 -or $offset + $original.Length -gt $dll.Length) { throw 'Invalid scanner DLL patch offset.' }
[Array]::Copy($original, 0, $dll, $offset, $original.Length)
$approved = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'logger-fix.json') -Raw | ConvertFrom-Json)[0].after_sha256
if ((Get-BytesHash $dll) -ne $audit.before_sha256 -or (Get-BytesHash $dll) -ne $approved) { throw 'Original scanner DLL checksum mismatch.' }
Write-Dependency 'runtime/ucrt64/bin/libsane-1.dll' $dll
$pdf = Read-Resource 'PdfSharp.dll'
Write-Dependency 'app/vendor/PDFsharp-1.50.5147/lib/net20/PdfSharp.dll' $pdf
Write-Dependency 'app/PdfSharp.dll' $pdf
Write-Dependency 'downloads/zadig-2.9.exe' ([IO.File]::ReadAllBytes($zadigPath))
Write-Output 'Build dependencies restored and verified. No user documents were copied.'
