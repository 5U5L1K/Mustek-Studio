"""Read the split Wise installer without executing its code.

Recover only raw DEFLATE streams whose following CRC32 matches. Filenames
are deliberately not inferred; the installation script remains available.
"""
from pathlib import Path
import json
import struct
import zlib

ROOT = Path(__file__).resolve().parent
parts = [ROOT / '120UBPlus_9x_2k_xp' / 'web.EXE']
parts += sorted((ROOT / '120UBPlus_9x_2k_xp').glob('web.W*'))
data = b''.join(p.read_bytes() for p in parts)
out = ROOT / 'legacy_extracted'
out.mkdir(exist_ok=True)
records = []
i = 0
while i < len(data) - 8:
    # All DEFLATE blocks start with a three-bit block header; reserved type
    # cannot be a valid stream. A short probe avoids expanding random data.
    if ((data[i] >> 1) & 3) == 3:
        i += 1
        continue
    try:
        dec = zlib.decompressobj(-15)
        probe = dec.decompress(data[i:i+256], 1_048_576)
        if len(probe) < 8:
            i += 1
            continue
        dec = zlib.decompressobj(-15)
        blob = dec.decompress(data[i:i+16_777_216], 33_554_432)
        if not dec.eof:
            i += 1
            continue
        used = min(16_777_216, len(data)-i) - len(dec.unused_data)
        crc = zlib.crc32(blob)
        tail = data[i+used:i+used+5]
        padding = next((n for n in (0, 1) if len(tail) >= n+4
                        and struct.unpack_from('<I', tail, n)[0] == crc), None)
        if padding is None:
            i += 1
            continue
        name = f'{len(records):04d}_{i:08x}.bin'
        (out / name).write_bytes(blob)
        record = dict(file=name, offset=i, compressed_size=used,
                      size=len(blob), crc32=f'{crc:08x}')
        records.append(record)
        print(json.dumps(record), flush=True)
        i += used + padding + 4
    except zlib.error:
        i += 1
(out / 'manifest.json').write_text(json.dumps(records, indent=2))
print(f'Recovered {len(records)} CRC-verified streams')
