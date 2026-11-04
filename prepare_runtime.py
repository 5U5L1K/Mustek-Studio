"""Download MSYS2 runtime DLLs into this project; no system installation."""
from pathlib import Path
import hashlib
import html
import json
import re
import subprocess
import urllib.request

ROOT = Path(__file__).resolve().parent
RUNTIME = ROOT / 'runtime'
BIN = RUNTIME / 'ucrt64/bin'
OBJDUMP = r'C:\msys64\ucrt64\bin\objdump.exe'
SEVEN = r'C:\Program Files\7-Zip\7z.exe'
PACKAGES = {
    'libieee1284-3.dll': 'libieee1284',
    'libusb-1.0.dll': 'libusb', 'libjpeg-8.dll': 'libjpeg-turbo',
    'libpng16-16.dll': 'libpng', 'libxml2-16.dll': 'libxml2',
    'zlib1.dll': 'zlib', 'libiconv-2.dll': 'libiconv',
    'libcharset-1.dll': 'libiconv', 'liblzma-5.dll': 'xz',
    'libzstd.dll': 'zstd', 'libbz2-1.dll': 'bzip2',
    'libgcc_s_seh-1.dll': 'gcc-libs', 'libstdc++-6.dll': 'gcc-libs',
    'libwinpthread-1.dll': 'libwinpthread',
}
manifest_path = ROOT / 'downloads/dependencies.json'
manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else []
installed = set()
def download_package(short):
    name = 'mingw-w64-ucrt-x86_64-' + short
    page_url = 'https://packages.msys2.org/packages/' + name
    with urllib.request.urlopen(page_url, timeout=45) as response:
        page = html.unescape(response.read().decode())
    match = re.search(r'https://mirror\.msys2\.org/mingw/ucrt64/'
                      + re.escape(name) + r'-[^"<>\s]+\.pkg\.tar\.zst', page)
    if not match:
        raise RuntimeError('Package URL missing: ' + page_url)
    url = match.group()
    sha = re.search(r'SHA256:.*?([0-9a-f]{64})', page, re.S).group(1)
    dest = ROOT / 'downloads' / url.rsplit('/', 1)[1]
    print('Downloading', url, flush=True)
    if not dest.exists() or hashlib.sha256(dest.read_bytes()).hexdigest() != sha:
        with urllib.request.urlopen(url, timeout=60) as response:
            dest.write_bytes(response.read())
    if hashlib.sha256(dest.read_bytes()).hexdigest() != sha:
        raise RuntimeError('Checksum mismatch: ' + str(dest))
    subprocess.run([SEVEN, 'x', str(dest), '-o' + str(dest.parent), '-y'],
                   check=True, capture_output=True)
    subprocess.run(['tar', '-xf', str(dest.with_suffix('')), '-C', str(RUNTIME)],
                   check=True, capture_output=True)
    manifest.append(dict(package=name, url=url, sha256=sha, page=page_url))
    (ROOT / 'downloads/dependencies.json').write_text(json.dumps(manifest, indent=2))
    installed.add(short)

def imports(path):
    proc = subprocess.run([OBJDUMP, '-p', str(path)], check=True,
                          capture_output=True, text=True)
    return re.findall(r'DLL Name: (\S+)', proc.stdout)

queue = [BIN / 'scanimage.exe', BIN / 'sane-find-scanner.exe',
         BIN / 'libsane-1.dll', RUNTIME / 'ucrt64/lib/bin/libsane-gt68xx-1.dll']
seen = set()
while queue:
    path = queue.pop(0)
    if path.name.lower() in seen:
        continue
    seen.add(path.name.lower())
    for dll in imports(path):
        key = dll.lower()
        if key.startswith(('api-ms-', 'ext-ms-')) or (Path(r'C:\Windows\System32') / dll).exists():
            continue
        local = BIN / dll
        if not local.exists():
            package = PACKAGES.get(key)
            if not package or package in installed:
                raise RuntimeError('Unresolved DLL: ' + dll)
            download_package(package)
        queue.append(local)
print('Dependency check complete:', len(seen), 'files', flush=True)
