"""Repair a verified GT68xx binary's Win64 timeval/localtime ABI mismatch.

The only localtime call in this backend is sanei_debug_msg(), passing a
pointer to Windows timeval.tv_sec (a 32-bit long). MSYS2's binary imports
_localtime64, which reads both tv_sec and tv_usec as one time_t and returns
NULL. Bind that import to _localtime32, matching the actual argument.
Only these exact upstream bytes are accepted; the system CRT is unchanged.
Original files are preserved, and re-running the fix is harmless.
"""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parent
ORIGINAL_SHA256 = '4e11b45c8c017098990e15b81e829cacff9b6736a24523a028fcd8c51fc71dde'
OLD = b'\0_localtime64\0'
NEW = b'\0_localtime32\0'

def fix_logger():
    paths = [ROOT / 'runtime/ucrt64/bin/libsane-1.dll',
             ROOT / 'runtime/ucrt64/lib/bin/libsane-gt68xx-1.dll']
    records = []
    for path in paths:
        data = path.read_bytes()
        already_fixed = OLD not in data and data.count(NEW) == 1
        original = data.replace(NEW, OLD) if already_fixed else data
        if hashlib.sha256(original).hexdigest() != ORIGINAL_SHA256:
            raise RuntimeError('Unrecognized binary; refusing to patch: ' + str(path))
        if original.count(OLD) != 1:
            raise RuntimeError('Expected exactly one localtime import')
        fixed = original.replace(OLD, NEW)
        backup = path.with_suffix('.dll.before-time-fix')
        if not backup.exists():
            backup.write_bytes(original)
        if not already_fixed:
            path.write_bytes(fixed)
        records.append(dict(file=str(path.relative_to(ROOT)),
                            before_sha256=ORIGINAL_SHA256,
                            after_sha256=hashlib.sha256(fixed).hexdigest(),
                            import_offset=original.index(OLD)+1,
                            old_import='_localtime64', new_import='_localtime32'))
        print('Fixed logger ABI:', path.name)
    (ROOT / 'logger-fix.json').write_text(json.dumps(records, indent=2))

if __name__ == '__main__':
    fix_logger()
