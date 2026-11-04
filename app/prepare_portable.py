"""Build deterministic, verified assets for the single-file portable EXE."""
from pathlib import Path
import hashlib
import io
import json
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parent.parent
BUILD = ROOT / 'app' / 'portable-build'
BUILD.mkdir(exist_ok=True)

def digest(data):
    return hashlib.sha256(data).hexdigest()

def archive(files):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for name, data in sorted(files.items()):
            info = zipfile.ZipInfo(name, (2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            z.writestr(info, data, compresslevel=9)
    return stream.getvalue()

payload = {}
native = ['scanimage.exe', 'libsane-1.dll', 'libjpeg-8.dll', 'libpng16-16.dll',
          'libusb-1.0.dll', 'libxml2-16.dll', 'libiconv-2.dll', 'zlib1.dll']
for name in native:
    relative = 'runtime/ucrt64/bin/' + name
    payload[relative] = (ROOT / relative).read_bytes()

# Only the portable copy is patched. Native SANE joins its data directory
# with sane/gt68xx/SBfw.usb. Making that directory "." removes build-machine
# paths and avoids narrowing Unicode working-directory names to an ANSI path.
name = 'runtime/ucrt64/bin/libsane-1.dll'
original = payload[name]
approved = json.loads((ROOT / 'logger-fix.json').read_text())[0]['after_sha256']
if digest(original) != approved:
    raise RuntimeError('GT68xx DLL differs from the verified working build')
old = b'D:/M/msys64/ucrt64/share'
if original.count(old) != 1:
    raise RuntimeError('Expected exactly one SANE data directory')
payload[name] = original.replace(old, b'.' + b'\0' * (len(old) - 1))
audit = dict(file=name, offset=original.index(old), old_data_directory=old.decode(),
             new_data_directory='.', before_sha256=digest(original),
             after_sha256=digest(payload[name]))
payload['portable-data-directory-fix.json'] = json.dumps(audit, indent=2).encode()
payload['sane/gt68xx/SBfw.usb'] = (ROOT / 'firmware/SBfw.usb').read_bytes()

license_root = ROOT / 'runtime/ucrt64/share/licenses'
for path in license_root.rglob('*'):
    if path.is_file():
        payload['licenses/' + path.relative_to(license_root).as_posix()] = path.read_bytes()
payload['licenses/PDFsharp-LICENSE.txt'] = (ROOT / 'app/PDFsharp-LICENSE.txt').read_bytes()
for relative in ['runtime/ucrt64/share/doc/sane-backends/COPYING',
                 'runtime/ucrt64/share/doc/sane-backends/LICENSE']:
    payload['licenses/sane/' + Path(relative).name] = (ROOT / relative).read_bytes()
payload['README.txt'] = (ROOT / 'app/PORTABLE-README.txt').read_bytes()
for relative in ['logger-fix.json', 'binary-output-fix.json', 'fix_logger.py',
                 'fix_binary_output.py', 'patches/win64-timeval.patch',
                 'patches/windows-binary-output.patch', 'downloads/dependencies.json']:
    payload['source/' + relative] = (ROOT / relative).read_bytes()
payload['source/backends-1.4.0.tar.bz2'] = (ROOT / 'downloads/backends-1.4.0.tar.bz2').read_bytes()
application_source = {p.name: p.read_bytes() for p in (ROOT / 'app').iterdir()
                      if p.is_file() and p.suffix.lower() in ('.cs', '.ps1', '.py', '.manifest', '.md', '.txt')}
application_source['assets/mustek-scanner-app-icon.ico'] = (ROOT / 'app/assets/mustek-scanner-app-icon.ico').read_bytes()
payload['source/application.zip'] = archive(application_source)

manifest = ''.join(f'{digest(data)}\t{len(data)}\t{name}\n' for name, data in sorted(payload.items()))
package = archive(payload)
(BUILD / 'payload.zip').write_bytes(package)
(BUILD / 'manifest.tsv').write_text(manifest, encoding='utf-8', newline='\n')
(BUILD / 'version.txt').write_text(digest(package), encoding='ascii')

# Carry interface/scan preferences into a fresh portable profile. User page
# files, filenames, logs and scans are not embedded in the release binary.
defaults = dict(ScanDpi='150', PreviewDpi='75', Mode='Color', Format='PDF',
                A4='true', Language='ru', Theme='light')
allowed = dict(ScanDpi={'50', '75', '150', '300', '600', '1200'},
               PreviewDpi={'50', '75', '150', '300', '600', '1200'},
               Mode={'Color', 'Gray'}, Format={'PNG', 'PDF'},
               A4={'true', 'false'}, Language={'ru', 'en'}, Theme={'light', 'dark'})
state = ROOT / 'app-data/document.xml'
if state.exists():
    saved = ET.parse(state).getroot()
    for key in defaults:
        value = saved.findtext(key)
        if value in allowed[key]:
            defaults[key] = value
document = ET.Element('DocumentState')
for key, value in defaults.items():
    ET.SubElement(document, key).text = value
ET.SubElement(document, 'Pages')
(BUILD / 'defaults.xml').write_bytes(ET.tostring(document, encoding='utf-8', xml_declaration=True))
(BUILD / 'assets.json').write_text(json.dumps(dict(version=digest(package), files=len(payload),
    payload_bytes=len(package), native_files=native, native_patch=audit), indent=2), encoding='utf-8')
print('Payload:', len(payload), 'files,', len(package), 'bytes')
print('SHA256:', digest(package))
