"""Prepare the GT68xx-only SANE frontend and recover the matching firmware."""
from pathlib import Path
import hashlib
import json
import shutil
import struct

ROOT = Path(__file__).resolve().parent
legacy = ROOT / 'legacy_extracted'
manifest = json.loads((legacy / 'manifest.json').read_text())
script = (legacy / manifest[0]['file']).read_bytes()
needle = b'%WIN%\\System32\\Drivers\\SBfw.usb\0'
position = script.index(needle)
# Wise file record: stream start/end, DOS timestamp, uncompressed length,
# followed by the NUL-terminated destination. Offsets are payload-relative.
start, end, dos_time, size = struct.unpack_from('<4I', script, position-16)
base = manifest[2]['offset']
matches = [r for r in manifest if r['offset'] == start+base and r['size'] == size
           and r['offset'] + r['compressed_size'] + 4 == end+base]
if len(matches) != 1:
    raise RuntimeError('Firmware record could not be verified against installer')
record = matches[0]
firmware = (legacy / record['file']).read_bytes()
destination = ROOT / 'firmware/SBfw.usb'
destination.parent.mkdir(exist_ok=True)
destination.write_bytes(firmware)
record = dict(record, destination='firmware/SBfw.usb',
              sha256=hashlib.sha256(firmware).hexdigest(),
              installer_path=needle[:-1].decode(), script_offset=position)
(destination.parent / 'provenance.json').write_text(json.dumps(record, indent=2))
bin_dir = ROOT / 'runtime/ucrt64/bin'
dispatcher = bin_dir / 'libsane-1.dll'
backup = bin_dir / 'libsane-dispatcher-original.dll'
if not backup.exists():
    shutil.copy2(dispatcher, backup)
# Both DLLs implement the public SANE ABI. Bind scanimage directly to the
# GT68xx module, because this MSYS2 dispatcher was built without dl support.
backend = ROOT / 'runtime/ucrt64/lib/bin/libsane-gt68xx-1.dll'
shutil.copy2(backend, dispatcher)
print('Firmware verified:', record)
print('scanimage now binds directly to the GT68xx SANE ABI')
from fix_logger import fix_logger
fix_logger()
from fix_binary_output import fix_binary_output
fix_binary_output()
