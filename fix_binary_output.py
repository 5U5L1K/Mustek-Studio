"""Use binary output in the verified MSYS2 scanimage Windows build.

Upstream scanimage.c opens output files with fopen(..., "w"). On Windows
that inserts CR before every LF, corrupting PNG bytes. Its shared "w"
literal has alignment padding, so it can be changed to "wb" in place.
The full original hash, uniqueness, and padding are verified first.
"""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parent
ORIGINAL_SHA256 = 'a01eec69a79e2fab41cab8d07c36c265f01dd510d13d71768f4260b54f5c5cb1'
OFFSET = 95488

def fix_binary_output():
    path = ROOT / 'runtime/ucrt64/bin/scanimage.exe'
    data = path.read_bytes()
    original = bytearray(data)
    if data[OFFSET:OFFSET+3] == b'wb\0':
        original[OFFSET:OFFSET+3] = b'w\0\0'
    if hashlib.sha256(original).hexdigest() != ORIGINAL_SHA256:
        raise RuntimeError('Unrecognized scanimage binary; refusing to patch')
    if original.count(b'\0w\0') != 1 or original[OFFSET:OFFSET+8] != b'w\0\0\0\0\0\0\0':
        raise RuntimeError('Output mode or its alignment padding changed')
    fixed = bytearray(original)
    fixed[OFFSET:OFFSET+3] = b'wb\0'
    backup = path.with_suffix('.exe.before-binary-fix')
    if not backup.exists():
        backup.write_bytes(original)
    if data != fixed:
        path.write_bytes(fixed)
    record = dict(file=str(path.relative_to(ROOT)), offset=OFFSET,
                  before_sha256=ORIGINAL_SHA256,
                  after_sha256=hashlib.sha256(fixed).hexdigest(),
                  old_mode='w', new_mode='wb')
    (ROOT / 'binary-output-fix.json').write_text(json.dumps(record, indent=2))
    print('scanimage output mode: binary')

if __name__ == '__main__':
    fix_binary_output()
